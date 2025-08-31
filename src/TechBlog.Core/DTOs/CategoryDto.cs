using System.Collections.Generic;

namespace TechBlog.Core.DTOs
{
    public class CategoryDto : BaseDto
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public int PostCount { get; set; }
        public int PostsCount { get; set; }
    }

    public class CategoryDetailDto : CategoryDto
    {
        public ICollection<PostListDto> Posts { get; set; } = new List<PostListDto>();
    }

    public class CreateCategoryDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class UpdateCategoryDto : CreateCategoryDto
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public int PostsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
