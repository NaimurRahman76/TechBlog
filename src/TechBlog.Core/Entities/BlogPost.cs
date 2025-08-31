using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechBlog.Core.Entities
{
    public class BlogPost : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Slug { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        [StringLength(500)]
        public string Summary { get; set; }
        
        public int ViewCount { get; set; } = 0;
        
        public bool IsPublished { get; set; } = false;
        
        public DateTime? PublishedAt { get; set; }
        
        [StringLength(500)]
        public string? FeaturedImageUrl { get; set; }
        
        // Foreign keys
        public int CategoryId { get; set; }
        
        public string AuthorId { get; set; }
        
        // Navigation properties
        public virtual Category Category { get; set; }
        public virtual ApplicationUser Author { get; set; }
        public virtual ICollection<BlogPostTag> BlogPostTags { get; set; } = new List<BlogPostTag>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
