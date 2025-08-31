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

            // Ensure the slug is unique
            existingCategory.Name = category.Name;
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

        public async Task<bool> CategorySlugExistsAsync(string slug, int? excludeId = null)
        {
            return await _context.Categories
                .AnyAsync(c => c.Slug == slug && 
                             !c.IsDeleted && 
                             (!excludeId.HasValue || c.Id != excludeId.Value));
        }

        public async Task<int> GetTotalCategoriesCountAsync()
        {
            return await _context.Categories.CountAsync(c => !c.IsDeleted);
        }

        private async Task<string> GenerateUniqueSlugAsync(string name, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name cannot be empty.", nameof(name));

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
                slug = "category";

            // Make sure the slug is unique
            string uniqueSlug = slug;
            int counter = 1;

            while (await CategorySlugExistsAsync(uniqueSlug, excludeId))
            {
                uniqueSlug = $"{slug}-{counter}";
                counter++;
            }

            return uniqueSlug;
        }
    }
}
