using System.Collections.Generic;
using System.Threading.Tasks;
using TechBlog.Core.DTOs;
using TechBlog.Core.Entities;

namespace TechBlog.Core.Interfaces
{
    public interface ITagService
    {
        Task<IEnumerable<Tag>> GetAllTagsAsync();
        Task<Tag> GetTagByIdAsync(int id);
        Task<Tag> GetTagBySlugAsync(string slug);
        Task<Tag> CreateTagAsync(Tag tag);
        Task UpdateTagAsync(Tag tag);
        Task DeleteTagAsync(int id);
        Task<bool> TagExistsAsync(int id);
        Task<bool> TagSlugExistsAsync(string slug, int? excludeId = null);
        Task<IEnumerable<Tag>> GetOrCreateTagsByNamesAsync(string[] tagNames);
        Task<int> GetTotalTagsCountAsync();
        Task<IEnumerable<TagDto>> GetPopularTagsAsync(int count);
    }
}
