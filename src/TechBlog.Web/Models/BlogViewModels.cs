using TechBlog.Core.DTOs;

namespace TechBlog.Web.Models
{
    public class BlogIndexViewModel
    {
        public IEnumerable<PostListDto> Posts { get; set; } = new List<PostListDto>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public string? SearchTerm { get; set; }
    }

    public class BlogPostViewModel
    {
        public PostDetailDto Post { get; set; } = new PostDetailDto();
        public IEnumerable<PostListDto> RelatedPosts { get; set; } = new List<PostListDto>();
    }

    public class BlogCategoryViewModel
    {
        public CategoryDto Category { get; set; } = new CategoryDto();
        public IEnumerable<PostListDto> Posts { get; set; } = new List<PostListDto>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
    }

    public class BlogTagViewModel
    {
        public TagDto Tag { get; set; } = new TagDto();
        public IEnumerable<PostListDto> Posts { get; set; } = new List<PostListDto>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
    }
}
