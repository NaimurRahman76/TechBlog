using System;
using System.ComponentModel.DataAnnotations;

namespace TechBlog.Core.Entities
{
    public class RecaptchaSettings
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string SiteKey { get; set; }
        
        [Required]
        [StringLength(100)]
        public string SecretKey { get; set; }
        
        public bool IsEnabled { get; set; }
        
        [Display(Name = "Enable for Login")]
        public bool EnableForLogin { get; set; }
        
        [Display(Name = "Enable for Registration")]
        public bool EnableForRegistration { get; set; }
        
        [Display(Name = "Enable for Comments")]
        public bool EnableForComments { get; set; }
        
        [Range(0.1, 1.0)]
        [Display(Name = "Score Threshold")]
        public float ScoreThreshold { get; set; } = 0.5f;
        
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }
    }
}
