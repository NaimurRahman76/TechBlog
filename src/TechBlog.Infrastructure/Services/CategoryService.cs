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
    public class CategoryService : BaseService, ICategoryService
    {
        public CategoryService(
            ApplicationDbContext context,
            ILogger<CategoryService> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .Include(c => c.BlogPosts)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<Category> GetCategoryBySlugAsync(string slug)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.Slug == slug);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            // Ensure the name is unique by auto-generating if needed
            category.Name = await GenerateUniqueNameAsync(category.Name);

            // Ensure the slug is unique
            category.Slug = await GenerateUniqueSlugAsync(category.Slug ?? category.Name);
            
            _context.Categories.Add(category);
            await SaveChangesAsync();
            return category;
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            var existingCategory = await GetCategoryByIdAsync(category.Id);
            if (existingCategory == null)
                throw new KeyNotFoundException($"Category with ID {category.Id} not found.");

            // Ensure the name is unique by auto-generating if needed
            existingCategory.Name = await GenerateUniqueNameAsync(category.Name, category.Id);
            existingCategory.Description = category.Description;
            existingCategory.Slug = await GenerateUniqueSlugAsync(category.Slug ?? category.Name, category.Id);
            existingCategory.UpdatedAt = DateTime.UtcNow;

            await SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await GetCategoryByIdAsync(id);
            if (category == null)
                throw new KeyNotFoundException($"Category with ID {id} not found.");

            // Check if the category is being used by any posts
            var isInUse = await _context.BlogPosts.AnyAsync(p => p.CategoryId == id);
            if (isInUse)
                throw new InvalidOperationException("Cannot delete a category that is being used by one or more posts.");

            // Soft delete
            category.IsDeleted = true;
            category.UpdatedAt = DateTime.UtcNow;
            await SaveChangesAsync();
        }

        public async Task<bool> CategoryExistsAsync(int id)
        {
            return await _context.Categories.AnyAsync(c => c.Id == id);
        }

        public async Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null)
        {
            return await _context.Categories
                .AnyAsync(c => c.Name == name &&
                             !c.IsDeleted &&
                             (!excludeId.HasValue || c.Id != excludeId.Value));
        }

        public async Task<bool> CategorySlugExistsAsync(string slug, int? excludeId = null)
        {
            return await _context.Categories
                .AnyAsync(c => c.Slug == slug && 
                             !c.IsDeleted &&
                             (!excludeId.HasValue || c.Id != excludeId.Value));
        }

        public async Task<int> GetTotalCategoriesCountAsync()
        {
            return await _context.Categories.CountAsync();
        }

        public async Task<Category> GetCategoryByIdWithPostsAsync(int id, bool includeUnpublished = false)
        {
            var query = _context.Categories
                .Include(c => c.BlogPosts)
                    .ThenInclude(p => p.Author)
                .AsNoTracking()
                .AsQueryable();

            if (!includeUnpublished)
            {
                query = query.Where(c => c.BlogPosts.Any(p => p.IsPublished));
            }

            return await query.FirstOrDefaultAsync(c => c.Id == id);
        }

        private async Task<string> GenerateUniqueNameAsync(string name, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name cannot be empty.", nameof(name));

            // If the name is already unique, return it as-is
            if (!await CategoryNameExistsAsync(name, excludeId))
                return name;

            // Generate unique name by adding numbers
            string uniqueName = name;
            int counter = 1;
            while (await CategoryNameExistsAsync(uniqueName, excludeId))
            {
                uniqueName = $"{name}{counter}";
                counter++;

                // Safety check to prevent infinite loops
                if (counter > 1000)
                    throw new InvalidOperationException("Unable to generate a unique category name after 1000 attempts.");
            }

            return uniqueName;
        }

        private async Task<string> GenerateUniqueSlugAsync(string slug, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug cannot be empty.", nameof(slug));

            // Convert to URL-friendly slug
            var urlFriendlySlug = slug.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("&", "and")
                .Replace("#", "sharp")
                .Replace("+", "plus");

            // Remove invalid characters
            urlFriendlySlug = new string(urlFriendlySlug
                .Where(c => char.IsLetterOrDigit(c) || c == '-')
                .ToArray());

            // Remove duplicate dashes
            while (urlFriendlySlug.Contains("--"))
            {
                urlFriendlySlug = urlFriendlySlug.Replace("--", "-");
            }

            // Trim dashes from beginning and end
            urlFriendlySlug = urlFriendlySlug.Trim(' ', '-');

            // If the slug is already unique, return it as-is
            if (!await CategorySlugExistsAsync(urlFriendlySlug, excludeId))
                return urlFriendlySlug;

            // Generate unique slug by adding numbers
            string uniqueSlug = urlFriendlySlug;
            int counter = 1;

            while (await CategorySlugExistsAsync(uniqueSlug, excludeId))
            {
                uniqueSlug = $"{urlFriendlySlug}-{counter}";
                counter++;

                // safety check to prevent infinite loops
                if (counter > 1000)
                    throw new InvalidOperationException("Unable to generate a unique slug after 1000 attempts.");
            }

            return uniqueSlug;
        }
    }
}
