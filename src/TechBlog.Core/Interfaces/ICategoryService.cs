using System.Collections.Generic;
using System.Threading.Tasks;
using TechBlog.Core.Entities;

namespace TechBlog.Core.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<Category> GetCategoryByIdAsync(int id);
        Task<Category> GetCategoryBySlugAsync(string slug);
        Task<Category> CreateCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(int id);
        Task<bool> CategoryExistsAsync(int id);
        Task<bool> CategorySlugExistsAsync(string slug, int? excludeId = null);
        Task<int> GetTotalCategoriesCountAsync();
        Task<Category> GetCategoryByIdWithPostsAsync(int id, bool includeUnpublished = false);
    }
}
