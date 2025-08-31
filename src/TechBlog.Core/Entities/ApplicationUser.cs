using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechBlog.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Bio { get; set; }
        
        [StringLength(200)]
        public string? ProfileImageUrl { get; set; }
        
        // Navigation properties
        public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
