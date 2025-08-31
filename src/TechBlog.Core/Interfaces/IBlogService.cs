using System.Collections.Generic;
using System.Threading.Tasks;
using TechBlog.Core.Entities;

namespace TechBlog.Core.Interfaces
{
    public interface IBlogService
    {
        // Post operations
        Task<IEnumerable<BlogPost>> GetAllPostsAsync(bool includeUnpublished = false);
        Task<BlogPost> GetPostByIdAsync(int id);
        Task<BlogPost> GetPostBySlugAsync(string slug);
        Task<IEnumerable<BlogPost>> GetPostsByCategoryAsync(int categoryId);
        Task<IEnumerable<BlogPost>> GetPostsByTagAsync(int tagId);
        Task<IEnumerable<BlogPost>> SearchPostsAsync(string searchTerm);
        Task<BlogPost> CreatePostAsync(BlogPost post, string[] tagNames);
        Task UpdatePostAsync(BlogPost post, string[] tagNames);
        Task DeletePostAsync(int id);
        Task<bool> PostExistsAsync(int id);
        Task IncrementViewCountAsync(int postId);
        
        // Additional methods needed by controllers
        Task<int> GetTotalPostsCountAsync();
        Task<int> GetPublishedPostsCountAsync();
        Task<int> GetDraftPostsCountAsync();
        Task<IEnumerable<BlogPost>> GetRecentPostsAsync(int count, bool includeUnpublished = false);
        Task<IEnumerable<BlogPost>> GetPopularPostsAsync(int count, int days = 30);
        Task<IEnumerable<BlogPost>> GetRelatedPostsAsync(int postId, int count);
        Task<bool> PostSlugExistsAsync(string slug, int? excludeId = null);
    }
}
