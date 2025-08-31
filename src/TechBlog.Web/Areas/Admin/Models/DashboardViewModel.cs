using TechBlog.Core.DTOs;

namespace TechBlog.Web.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public int TotalPosts { get; set; }
        public int PublishedPosts { get; set; }
        public int DraftPosts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalTags { get; set; }
        public IEnumerable<PostListDto> RecentPosts { get; set; } = new List<PostListDto>();
        public int TotalViews { get; set; }
        public int TotalComments { get; set; }
    }
}
