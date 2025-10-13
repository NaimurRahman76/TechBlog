using System;
using System.Collections.Generic;

namespace TechBlog.Core.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Slug { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int PostsCount { get; set; }
    }

    public class PostListItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPublished { get; set; }
    }

    public class CategoryDetailDto : CategoryDto
    {
        public ICollection<PostListItemDto> RecentPosts { get; set; } = new List<PostListItemDto>();
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
