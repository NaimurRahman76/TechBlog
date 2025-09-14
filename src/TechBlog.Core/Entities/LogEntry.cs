using System;
using System.ComponentModel.DataAnnotations;

namespace TechBlog.Core.Entities
{
    public class LogEntry : BaseEntity
    {
        [Required]
        [StringLength(20)]
        public string Level { get; set; } = "Info"; // Info, Warning, Error

        [Required]
        [StringLength(200)]
        public string Source { get; set; } = "App"; // service/controller name

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Exception { get; set; }

        [StringLength(100)]
        public string? UserId { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [StringLength(256)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
