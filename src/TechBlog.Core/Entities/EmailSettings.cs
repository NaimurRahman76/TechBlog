using System;
using System.ComponentModel.DataAnnotations;

namespace TechBlog.Core.Entities
{
    public class EmailSettings
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "SMTP Host")]
        public string SmtpHost { get; set; } = string.Empty;
        
        [Required]
        [Range(1, 65535)]
        [Display(Name = "SMTP Port")]
        public int SmtpPort { get; set; } = 587;
        
        [Required]
        [StringLength(200)]
        [Display(Name = "From Email")]
        [EmailAddress]
        public string FromEmail { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        [Display(Name = "From Name")]
        public string FromName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        
        [Display(Name = "Enable SSL")]
        public bool EnableSsl { get; set; } = true;
        
        [Display(Name = "Enable Email Verification")]
        public bool EnableEmailVerification { get; set; } = true;
        
        [Display(Name = "Is Enabled")]
        public bool IsEnabled { get; set; } = false;
        
        [Range(1, 72)]
        [Display(Name = "Verification Link Expiration (hours)")]
        public int VerificationLinkExpirationHours { get; set; } = 24;
        
        [Range(1, 72)]
        [Display(Name = "Password Reset Link Expiration (hours)")]
        public int PasswordResetLinkExpirationHours { get; set; } = 1;
        
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }
    }
}
