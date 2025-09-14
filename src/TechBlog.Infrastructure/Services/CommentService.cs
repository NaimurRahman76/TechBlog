using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TechBlog.Core.Entities;
using TechBlog.Core.Exceptions;
using TechBlog.Core.Interfaces;
using TechBlog.Infrastructure.Data;

namespace TechBlog.Infrastructure.Services
{
    public class CommentService : BaseService, ICommentService
    {
        private readonly IWorkContext _workContext;
        private readonly IMemoryCache _cache;
        private const string CommentCacheKey = "Comment_{0}";
        private const string PostCommentsCacheKey = "Post_{0}_Comments";
        private const string AllCommentsCacheKey = "AllComments";
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);
        private static string BoolKey(bool value) => value.ToString(); // 'True' or 'False'
        private static string ComposeAllCommentsKey(bool includeUnapproved) => $"{AllCommentsCacheKey}_{BoolKey(includeUnapproved)}";
        private static string ComposePostCommentsKey(int postId, bool includeUnapproved) => $"{string.Format(PostCommentsCacheKey, postId)}_{BoolKey(includeUnapproved)}";
        
        public CommentService(
            ApplicationDbContext context,
            ILogger<CommentService> logger,
            IWorkContext workContext,
            IMemoryCache cache)
            : base(context, logger)
        {
            _workContext = workContext ?? throw new ArgumentNullException(nameof(workContext));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public void InvalidateAllCommentsCache()
        {
            // Current key format ("True"/"False")
            _cache.Remove(ComposeAllCommentsKey(true));
            _cache.Remove(ComposeAllCommentsKey(false));
            // Legacy keys ("true"/"false") for backward compatibility
            _cache.Remove($"{AllCommentsCacheKey}_true");
            _cache.Remove($"{AllCommentsCacheKey}_false");
        }

        public void InvalidatePostCommentsCache(int postId)
        {
            // Current key format ("True"/"False")
            _cache.Remove(ComposePostCommentsKey(postId, true));
            _cache.Remove(ComposePostCommentsKey(postId, false));
            // Legacy keys ("true"/"false") for backward compatibility
            _cache.Remove($"{string.Format(PostCommentsCacheKey, postId)}_true");
            _cache.Remove($"{string.Format(PostCommentsCacheKey, postId)}_false");
        }
        
        public async Task<IEnumerable<Comment>> GetAllCommentsAsync()
        {
            return await GetAllCommentsAsync(false);
        }

        public async Task<IEnumerable<Comment>> GetAllCommentsAsync(bool includeUnapproved = false)
        {
            var cacheKey = ComposeAllCommentsKey(includeUnapproved);
            
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                
                var query = _context.Comments
                    .AsNoTracking()
                    .Include(c => c.BlogPost)
                    .Include(c => c.Author)
                    .Include(c => c.ParentComment)
                    .Include(c => c.Replies)
                    .Where(c => !c.IsDeleted);

                if (!includeUnapproved)
                {
                    query = query.Where(c => c.IsApproved);
                }

                return await query
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
            });
        }

        public async Task<Comment> GetCommentByIdAsync(int id)
        {
            var cacheKey = string.Format(CommentCacheKey, id);
            
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                
                var comment = await _context.Comments
                    .AsNoTracking()
                    .Include(c => c.BlogPost)
                    .Include(c => c.Author)
                    .Include(c => c.ParentComment)
                    .Include(c => c.Replies)
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (comment == null)
                {
                    throw new EntityNotFoundException($"Comment with ID {id} not found.");
                }

                return comment;
            });
        }

        public async Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(int postId)
        {
            return await GetCommentsByPostIdAsync(postId, false);
        }
        
        public async Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(int postId, bool includeUnapproved)
        {
            var cacheKey = ComposePostCommentsKey(postId, includeUnapproved);
            
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                
                var query = _context.Comments
                    .AsNoTracking()
                    .Include(c => c.Author)
                    .Include(c => c.Replies)
                    .Where(c => c.BlogPostId == postId && !c.IsDeleted);
                    
                if (!includeUnapproved)
                {
                    query = query.Where(c => c.IsApproved);
                }
                
                return await query
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
            });
        }
        
        public async Task<IEnumerable<Comment>> GetCommentsByAuthorAsync(string authorId)
        {
            return await _context.Comments
                .Include(c => c.BlogPost)
                .Where(c => c.AuthorId == authorId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
        
        public async Task<Comment> CreateCommentAsync(Comment comment)
        {
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            // Set default values
            comment.CreatedAt = DateTime.UtcNow;
            comment.UpdatedAt = DateTime.UtcNow;

            // Set author information if user is authenticated
            if (_workContext.IsAuthenticated)
            {
                var currentUser = await _workContext.GetCurrentUserAsync();
                if (currentUser != null)
                {
                    comment.AuthorId = currentUser.Id;
                    // Respect provided values; fallback to account info if missing
                    if (string.IsNullOrWhiteSpace(comment.AuthorName))
                    {
                        comment.AuthorName = currentUser.UserName;
                    }
                    if (string.IsNullOrWhiteSpace(comment.AuthorEmail))
                    {
                        comment.AuthorEmail = currentUser.Email;
                    }
                    
                    // Auto-approve comments from admins/authors
                    if (_workContext.IsAdmin || _workContext.IsAuthor)
                    {
                        comment.IsApproved = true;
                    }
                }
            }

            // Validate the comment
            if (string.IsNullOrWhiteSpace(comment.Content))
            {
                throw new ValidationException("Comment content cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(comment.AuthorName) || string.IsNullOrWhiteSpace(comment.AuthorEmail))
            {
                throw new ValidationException("Author name and email are required.");
            }

            try
            {
                _context.Comments.Add(comment);
                await SaveChangesWithLoggingAsync();

                // Invalidate related caches
                InvalidatePostCommentsCache(comment.BlogPostId);
                InvalidateAllCommentsCache();
                
                _logger.LogInformation("Comment {CommentId} created successfully by {AuthorName}", comment.Id, comment.AuthorName);
                
                return comment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                throw new ApplicationException("An error occurred while creating the comment.", ex);
            }
        }
        
        public async Task UpdateCommentAsync(Comment comment)
        {
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            var existingComment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == comment.Id && !c.IsDeleted);

            if (existingComment == null)
            {
                throw new EntityNotFoundException($"Comment with ID {comment.Id} not found.");
            }

            // Check permissions
            if (!await CanModifyComment(existingComment))
            {
                throw new UnauthorizedAccessException("You don't have permission to update this comment.");
            }

            // Only update allowed fields
            existingComment.Content = comment.Content;
            existingComment.IsApproved = comment.IsApproved;
            existingComment.UpdatedAt = DateTime.UtcNow;

            try
            {
                await SaveChangesWithLoggingAsync();

                // Invalidate caches
                _cache.Remove(string.Format(CommentCacheKey, comment.Id));
                InvalidatePostCommentsCache(comment.BlogPostId);
                InvalidateAllCommentsCache();
                
                _logger.LogInformation("Comment {CommentId} updated successfully", comment.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment {CommentId}", comment.Id);
                throw new ApplicationException("An error occurred while updating the comment.", ex);
            }
        }
        
        public async Task DeleteCommentAsync(int id)
        {
            // Load tracked entity to perform soft delete
            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (comment == null)
            {
                throw new EntityNotFoundException($"Comment with ID {id} not found.");
            }

            // Only allow admins, post authors, or comment authors to delete
            if (!_workContext.IsAdmin && 
                !await IsPostAuthor(comment.BlogPostId) && 
                comment.AuthorId != _workContext.UserId)
            {
                throw new UnauthorizedAccessException("You don't have permission to delete this comment.");
            }
            
            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.UtcNow;
            
            await SaveChangesWithLoggingAsync();
            
            // Invalidate caches
            _cache.Remove(string.Format(CommentCacheKey, id));
            InvalidatePostCommentsCache(comment.BlogPostId);
            InvalidateAllCommentsCache();
        }
        
        public async Task<bool> CommentExistsAsync(int id)
        {
            return await _context.Comments.AnyAsync(c => c.Id == id && !c.IsDeleted);
        }
        
        public async Task ApproveCommentAsync(int id)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
                
            if (comment == null)
            {
                throw new EntityNotFoundException($"Comment with ID {id} not found.");
            }

            // Only admins and post authors can approve comments
            if (!_workContext.IsAdmin && !await IsPostAuthor(comment.BlogPostId))
            {
                throw new UnauthorizedAccessException("You don't have permission to approve comments.");
            }

            comment.IsApproved = true;
            comment.UpdatedAt = DateTime.UtcNow;

            try
            {
                await SaveChangesWithLoggingAsync();
                
                // Invalidate caches
                _cache.Remove(string.Format(CommentCacheKey, id));
                InvalidatePostCommentsCache(comment.BlogPostId);
                InvalidateAllCommentsCache();
                
                _logger.LogInformation("Comment {CommentId} approved successfully", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving comment {CommentId}", id);
                throw new ApplicationException("An error occurred while approving the comment.", ex);
            }
        }
        
        public async Task UnapproveCommentAsync(int id)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
                
            if (comment == null)
            {
                throw new EntityNotFoundException($"Comment with ID {id} not found.");
            }

            // Only admins and post authors can unapprove comments
            if (!_workContext.IsAdmin && !await IsPostAuthor(comment.BlogPostId))
            {
                throw new UnauthorizedAccessException("You don't have permission to unapprove comments.");
            }

            comment.IsApproved = false;
            comment.UpdatedAt = DateTime.UtcNow;

            try
            {
                await SaveChangesWithLoggingAsync();
                
                // Invalidate caches
                _cache.Remove(string.Format(CommentCacheKey, id));
                _cache.Remove($"{string.Format(PostCommentsCacheKey, comment.BlogPostId)}_true");
                _cache.Remove($"{string.Format(PostCommentsCacheKey, comment.BlogPostId)}_false");
                _cache.Remove("AllComments_true");
                _cache.Remove("AllComments_false");
                
                _logger.LogInformation("Comment {CommentId} unapproved successfully", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unapproving comment {CommentId}", id);
                throw new ApplicationException("An error occurred while unapproving the comment.", ex);
            }
        }
        
        public async Task<int> GetCommentsCountAsync(bool onlyApproved = true)
        {
            var query = _context.Comments.Where(c => !c.IsDeleted);
            
            if (onlyApproved)
            {
                query = query.Where(c => c.IsApproved);
            }
            
            return await query.CountAsync();
        }
        
        private async Task<bool> IsPostAuthor(int postId)
        {
            if (!_workContext.IsAuthenticated)
                return false;
                
            var post = await _context.BlogPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == postId);
                
            return post?.AuthorId == _workContext.UserId;
        }
        
        private new async Task SaveChangesAsync()
        {
            await base.SaveChangesAsync();
        }
        
        private async Task SaveChangesWithLoggingAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while saving changes");
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error while saving changes");
                throw;
            }
        }
        
        private async Task<bool> CanModifyComment(Comment comment)
        {
            if (_workContext.IsAdmin || _workContext.IsAuthor)
                return true;
                
            if (!_workContext.IsAuthenticated)
                return false;
                
            var currentUser = await _workContext.GetCurrentUserAsync();
            return comment.AuthorId == currentUser?.Id;
        }
    }
}
