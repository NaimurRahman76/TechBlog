using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;
using TechBlog.Infrastructure.Data;

namespace TechBlog.Infrastructure.Services
{
    public class TagService : BaseService, ITagService
    {
        public TagService(
            ApplicationDbContext context,
            ILogger<TagService> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Tag>> GetAllTagsAsync()
        {
            return await _context.Tags
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<Tag> GetTagByIdAsync(int id)
        {
            return await _context.Tags.FindAsync(id);
        }

        public async Task<Tag> GetTagBySlugAsync(string slug)
        {
            return await _context.Tags
                .FirstOrDefaultAsync(t => t.Slug == slug);
        }

        public async Task<Tag> CreateTagAsync(Tag tag)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            // Ensure the name is unique by auto-generating if needed
            tag.Name = await GenerateUniqueNameAsync(tag.Name);

            // Ensure the slug is unique
            tag.Slug = await GenerateUniqueSlugAsync(tag.Slug ?? tag.Name);

            _context.Tags.Add(tag);
            await SaveChangesAsync();
            return tag;
        }

        public async Task UpdateTagAsync(Tag tag)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            var existingTag = await GetTagByIdAsync(tag.Id);
            if (existingTag == null)
                throw new KeyNotFoundException($"Tag with ID {tag.Id} not found.");

            // Ensure the name is unique by auto-generating if needed
            existingTag.Name = await GenerateUniqueNameAsync(tag.Name, tag.Id);
            existingTag.Slug = await GenerateUniqueSlugAsync(tag.Slug ?? tag.Name, tag.Id);
            existingTag.UpdatedAt = DateTime.UtcNow;

            await SaveChangesAsync();
        }

        public async Task DeleteTagAsync(int id)
        {
            var tag = await GetTagByIdAsync(id);
            if (tag == null)
                throw new KeyNotFoundException($"Tag with ID {id} not found.");

            // Check if the tag is being used by any posts
            var isInUse = await _context.BlogPostTags.AnyAsync(pt => pt.TagId == id);
            if (isInUse)
                throw new InvalidOperationException("Cannot delete a tag that is being used by one or more posts.");

            // Soft delete
            tag.IsDeleted = true;
            tag.UpdatedAt = DateTime.UtcNow;
            await SaveChangesAsync();
        }

        public async Task<bool> TagExistsAsync(int id)
        {
            return await _context.Tags.AnyAsync(t => t.Id == id);
        }

        public async Task<bool> TagNameExistsAsync(string name, int? excludeId = null)
        {
            return await _context.Tags
                .AnyAsync(t => t.Name == name &&
                             !t.IsDeleted &&
                             (!excludeId.HasValue || t.Id != excludeId.Value));
        }

        public async Task<bool> TagSlugExistsAsync(string slug, int? excludeId = null)
        {
            return await _context.Tags
                .AnyAsync(t => t.Slug == slug &&
                             !t.IsDeleted &&
                             (!excludeId.HasValue || t.Id != excludeId.Value));
        }

        public async Task<int> GetTotalTagsCountAsync()
        {
            return await _context.Tags.CountAsync(t => !t.IsDeleted);
        }

        public async Task<IEnumerable<Tag>> GetOrCreateTagsByNamesAsync(string[] tagNames)
        {
            if (tagNames == null || tagNames.Length == 0)
                return Enumerable.Empty<Tag>();

            var normalizedNames = tagNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!normalizedNames.Any())
                return Enumerable.Empty<Tag>();

            // Get existing tags
            var existingTags = await _context.Tags
                .Where(t => normalizedNames.Contains(t.Name))
                .ToListAsync();

            // Find names that don't exist yet
            var newTagNames = normalizedNames
                .Except(existingTags.Select(t => t.Name), StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Create new tags
            var newTags = new List<Tag>();
            foreach (var name in newTagNames)
            {
                var tag = new Tag
                {
                    Name = await GenerateUniqueNameAsync(name),
                    Slug = await GenerateUniqueSlugAsync(name)
                };
                _context.Tags.Add(tag);
                newTags.Add(tag);
            }

            if (newTags.Any())
            {
                await SaveChangesAsync();
            }

            return existingTags.Concat(newTags);
        }

        private async Task<string> GenerateUniqueSlugAsync(string name, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tag name cannot be empty.", nameof(name));

            var slug = name.ToLower()
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
                slug = "tag";

            // Make sure the slug is unique
            string uniqueSlug = slug;
            int counter = 1;

            while (await TagSlugExistsAsync(uniqueSlug, excludeId))
            {
                uniqueSlug = $"{slug}-{counter}";
                counter++;
            }

            return uniqueSlug;
        }

        private async Task<string> GenerateUniqueNameAsync(string name, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tag name cannot be empty.", nameof(name));

            // If the name is already unique, return it as-is
            if (!await TagNameExistsAsync(name, excludeId))
                return name;

            // Generate unique name by adding numbers
            string uniqueName = name;
            int counter = 1;

            while (await TagNameExistsAsync(uniqueName, excludeId))
            {
                uniqueName = $"{name}{counter}";
                counter++;

                // Prevent infinite loops by limiting the counter
                if (counter > 1000)
                    throw new InvalidOperationException("Unable to generate a unique tag name. Too many duplicates exist.");
            }

            return uniqueName;
        }
    }
}
