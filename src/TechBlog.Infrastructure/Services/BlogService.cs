using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;
using TechBlog.Infrastructure.Data;
using System.Linq.Expressions;

namespace TechBlog.Infrastructure.Services
{
    public class BlogService : BaseService, IBlogService
    {
        private readonly ITagService _tagService;

        public BlogService(
            ApplicationDbContext context,
            ILogger<BlogService> logger,
            ITagService tagService)
            : base(context, logger)
        {
            _tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
        }

        public async Task<IEnumerable<BlogPost>> GetAllPostsAsync(bool includeUnpublished = false)
        {
            var query = _context.BlogPosts
                .Include(p => p.Category)
                .Include(p => p.Author)
                .Include(p => p.BlogPostTags)
                    .ThenInclude(pt => pt.Tag)
                .AsQueryable();

            if (!includeUnpublished)
            {
                query = query.Where(p => p.IsPublished);
            }

            return await query.OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                            .ToListAsync();
        }

        public async Task<BlogPost> GetPostByIdAsync(int id)
        {
            return await _context.BlogPosts
                .Include(p => p.Category)
                .Include(p => p.Author)
                .Include(p => p.BlogPostTags)
                    .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<BlogPost> GetPostBySlugAsync(string slug)
        {
            return await _context.BlogPosts
                .Include(p => p.Category)
                .Include(p => p.Author)
                .Include(p => p.BlogPostTags)
                    .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.Slug == slug);
        }

        public async Task<IEnumerable<BlogPost>> GetPostsByCategoryAsync(int categoryId)
        {
            return await _context.BlogPosts
                .Where(p => p.CategoryId == categoryId && p.IsPublished)
                .Include(p => p.Category)
                .Include(p => p.Author)
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<BlogPost>> GetPostsByTagAsync(int tagId)
        {
            return await _context.BlogPostTags
                .Where(pt => pt.TagId == tagId)
                .Select(pt => pt.BlogPost)
                .Where(p => p.IsPublished)
                .Include(p => p.Category)
                .Include(p => p.Author)
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<BlogPost>> SearchPostsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllPostsAsync();

            searchTerm = searchTerm.ToLower();
            return await _context.BlogPosts
                .Where(p => p.IsPublished && 
                           (p.Title.ToLower().Contains(searchTerm) || 
                            p.Content.ToLower().Contains(searchTerm) ||
                            p.Summary.ToLower().Contains(searchTerm)))
                .Include(p => p.Category)
                .Include(p => p.Author)
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .ToListAsync();
        }

        public async Task<BlogPost> CreatePostAsync(BlogPost post, string[] tagNames)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));

            // Ensure the slug is unique
            post.Slug = await GenerateUniqueSlugAsync(post.Slug ?? post.Title);
            
            // Set published date if the post is being published
            if (post.IsPublished && !post.PublishedAt.HasValue)
            {
                post.PublishedAt = DateTime.UtcNow;
            }

            // Handle tags
            if (tagNames != null && tagNames.Length > 0)
            {
                var tags = await _tagService.GetOrCreateTagsByNamesAsync(tagNames);
                post.BlogPostTags = tags.Select(t => new BlogPostTag { Tag = t }).ToList();
            }

            _context.BlogPosts.Add(post);
            await SaveChangesAsync();
            return post;
        }

        public async Task UpdatePostAsync(BlogPost post, string[] tagNames)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));

            var existingPost = await GetPostByIdAsync(post.Id);
            if (existingPost == null)
                throw new KeyNotFoundException($"Post with ID {post.Id} not found.");

            // Update properties
            existingPost.Title = post.Title;
            // Only update slug if it's explicitly provided or if title changed significantly
            if (!string.IsNullOrEmpty(post.Slug) && post.Slug != existingPost.Slug)
            {
                existingPost.Slug = await GenerateUniqueSlugAsync(post.Slug, post.Id);
            }
            existingPost.Content = post.Content;
            existingPost.Summary = post.Summary;
            existingPost.CategoryId = post.CategoryId;
            existingPost.FeaturedImageUrl = post.FeaturedImageUrl;
            
            // Handle publish/unpublish
            if (existingPost.IsPublished != post.IsPublished)
            {
                existingPost.IsPublished = post.IsPublished;
                existingPost.PublishedAt = post.IsPublished ? DateTime.UtcNow : (DateTime?)null;
            }

            // Handle tags
            if (tagNames != null)
            {
                // Remove existing tags
                _context.BlogPostTags.RemoveRange(existingPost.BlogPostTags);
                
                // Add new tags
                if (tagNames.Length > 0)
                {
                    var tags = await _tagService.GetOrCreateTagsByNamesAsync(tagNames);
                    existingPost.BlogPostTags = tags.Select(t => new BlogPostTag { Tag = t }).ToList();
                }
            }

            existingPost.UpdatedAt = DateTime.UtcNow;
            await SaveChangesAsync();
        }

        public async Task DeletePostAsync(int id)
        {
            var post = await GetPostByIdAsync(id);
            if (post == null)
                throw new KeyNotFoundException($"Post with ID {id} not found.");

            // Soft delete
            post.IsDeleted = true;
            post.UpdatedAt = DateTime.UtcNow;
            await SaveChangesAsync();
        }

        public async Task<bool> PostExistsAsync(int id)
        {
            return await _context.BlogPosts.AnyAsync(p => p.Id == id);
        }

        public async Task IncrementViewCountAsync(int postId)
        {
            var post = await _context.BlogPosts.FindAsync(postId);
            if (post != null)
            {
                post.ViewCount++;
                await SaveChangesAsync();
            }
        }

        public async Task<int> GetTotalPostsCountAsync()
        {
            return await _context.BlogPosts.CountAsync(p => !p.IsDeleted);
        }

        public async Task<int> GetPublishedPostsCountAsync()
        {
            return await _context.BlogPosts.CountAsync(p => p.IsPublished && !p.IsDeleted);
        }

        public async Task<int> GetDraftPostsCountAsync()
        {
            return await _context.BlogPosts.CountAsync(p => !p.IsPublished && !p.IsDeleted);
        }

        public async Task<IEnumerable<BlogPost>> GetRecentPostsAsync(int count, bool includeUnpublished = false)
        {
            var query = _context.BlogPosts
                .Include(p => p.Category)
                .Include(p => p.Author)
                .Include(p => p.BlogPostTags)
                    .ThenInclude(pt => pt.Tag)
                .Where(p => !p.IsDeleted);

            if (!includeUnpublished)
            {
                query = query.Where(p => p.IsPublished);
            }

            return await query.OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                            .Take(count)
                            .ToListAsync();
        }

        public async Task<IEnumerable<BlogPost>> GetPopularPostsAsync(int count, int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            
            return await _context.BlogPosts
                .Include(p => p.Category)
                .Include(p => p.Author)
                .Where(p => p.IsPublished && !p.IsDeleted && 
                           (p.PublishedAt ?? p.CreatedAt) >= cutoffDate)
                .OrderByDescending(p => p.ViewCount)
                .ThenByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<BlogPost>> GetRelatedPostsAsync(int postId, int count)
        {
            var post = await GetPostByIdAsync(postId);
            if (post == null)
            {
                return Enumerable.Empty<BlogPost>();
            }

            // Get posts in the same category
            var relatedPosts = await _context.BlogPosts
                .Include(p => p.Category)
                .Include(p => p.Author)
                .Where(p => p.Id != postId && 
                           p.CategoryId == post.CategoryId && 
                           p.IsPublished && 
                           !p.IsDeleted)
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Take(count)
                .ToListAsync();

            // If not enough posts in the same category, get recent posts
            if (relatedPosts.Count < count)
            {
                var remainingCount = count - relatedPosts.Count;
                var recentPosts = await _context.BlogPosts
                    .Include(p => p.Category)
                    .Include(p => p.Author)
                    .Where(p => p.Id != postId && 
                               p.CategoryId != post.CategoryId && 
                               p.IsPublished && 
                               !p.IsDeleted)
                    .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                    .Take(remainingCount)
                    .ToListAsync();

                relatedPosts.AddRange(recentPosts);
            }

            return relatedPosts.Distinct().Take(count);
        }

        public async Task<bool> PostSlugExistsAsync(string slug, int? excludeId = null)
        {
            return await _context.BlogPosts
                .AnyAsync(p => p.Slug == slug && 
                             !p.IsDeleted && 
                             (!excludeId.HasValue || p.Id != excludeId.Value));
        }

        private async Task<string> GenerateUniqueSlugAsync(string title, int? excludeId = null)
        {
            var slug = title.ToLower()
                .Replace(" ", "-")
                .Replace("&", "and")
                .Replace("+", "plus")
                .Replace("#", "sharp");
            
            // Remove invalid characters
            slug = System.Text.RegularExpressions.Regex.Replace(slug, "[^a-z0-9-]", "");
            
            // Remove duplicate hyphens
            slug = System.Text.RegularExpressions.Regex.Replace(slug, "-{2,}", "-");
            
            // Trim hyphens from start and end
            slug = slug.Trim('-');

            // If empty, use a default slug
            if (string.IsNullOrEmpty(slug))
                slug = "untitled-post";

            // Make sure the slug is unique
            string uniqueSlug = slug;
            int counter = 1;

            while (await _context.BlogPosts.AnyAsync(p => 
                p.Slug == uniqueSlug && 
                (!excludeId.HasValue || p.Id != excludeId.Value)))
            {
                uniqueSlug = $"{slug}-{counter}";
                counter++;
            }

            return uniqueSlug;
        }
    }
}
