using System.Collections.Generic;

namespace TechBlog.Core.DTOs
{
    public class TagDto : BaseDto
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public int PostCount { get; set; }
        public int PostsCount { get; set; }
    }

    public class TagDetailDto : TagDto
    {
        public ICollection<PostListDto> Posts { get; set; } = new List<PostListDto>();
    }

    public class CreateTagDto
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
    }

    public class UpdateTagDto : CreateTagDto
    {
        public int Id { get; set; }
        public int PostsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
