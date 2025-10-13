using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechBlog.Core.DTOs
{
    public class PostListDto : BaseDto
    {
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Summary { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int ViewCount { get; set; }
        public string AuthorName { get; set; }
        public string CategoryName { get; set; }
        public string CategorySlug { get; set; }
        public string? FeaturedImageUrl { get; set; }
        public ICollection<string> Tags { get; set; } = new List<string>();
    }

    public class PostDetailDto : PostListDto
    {
        public string Content { get; set; }
        public int CategoryId { get; set; }
        public string AuthorId { get; set; }
        public ICollection<CommentDto> Comments { get; set; } = new List<CommentDto>();
    }

    public class CreatePostDto
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }
        
        public string? Slug { get; set; }
        
        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; }
        
        [Required(ErrorMessage = "Summary is required")]
        public string Summary { get; set; }
        
        public bool IsPublished { get; set; }
        
        [Required(ErrorMessage = "Please select a category")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid category")]
        public int CategoryId { get; set; }
        
        public string? Tags { get; set; }
        public string? FeaturedImageUrl { get; set; }
    }

    public class UpdatePostDto : CreatePostDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class PostAdminListDto : PostListDto
    {
        public string Status { get; set; }
        public int CommentCount { get; set; }
    }
}
