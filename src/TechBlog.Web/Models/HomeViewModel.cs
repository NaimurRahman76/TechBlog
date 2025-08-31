using TechBlog.Core.DTOs;

namespace TechBlog.Web.Models
{
    public class HomeViewModel
    {
        public IEnumerable<PostListDto> RecentPosts { get; set; } = new List<PostListDto>();
        public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    }
}
