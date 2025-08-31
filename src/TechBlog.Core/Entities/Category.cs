using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechBlog.Core.Entities
{
    public class Category : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [StringLength(100)]
        public string Slug { get; set; }
        
        // Navigation property
        public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
    }
}
