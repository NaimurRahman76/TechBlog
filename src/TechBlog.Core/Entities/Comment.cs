using System;
using System.ComponentModel.DataAnnotations;

namespace TechBlog.Core.Entities
{
    public class Comment : BaseEntity
    {
        [Required]
        public string Content { get; set; }
        
        [Required]
        [StringLength(100)]
        public string AuthorName { get; set; }
        
        [EmailAddress]
        [StringLength(100)]
        public string AuthorEmail { get; set; }
        
        public bool IsApproved { get; set; } = false;
        
        // Foreign keys
        public int BlogPostId { get; set; }
        public string AuthorId { get; set; }
        public int? ParentCommentId { get; set; }
        
        // Navigation properties
        public virtual BlogPost BlogPost { get; set; }
        public virtual ApplicationUser Author { get; set; }
        public virtual Comment ParentComment { get; set; }
        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}
