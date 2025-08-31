using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechBlog.Core.Entities
{
    public class Tag : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [StringLength(100)]
        public string Slug { get; set; }
        
        // Navigation property
        public virtual ICollection<BlogPostTag> BlogPostTags { get; set; } = new List<BlogPostTag>();
    }
}
