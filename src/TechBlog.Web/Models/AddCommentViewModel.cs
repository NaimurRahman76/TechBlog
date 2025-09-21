using System.ComponentModel.DataAnnotations;

namespace TechBlog.Web.Models
{
    public class AddCommentViewModel
    {
        public int PostId { get; set; }
        public int? ParentCommentId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string AuthorName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters")]
        public string AuthorEmail { get; set; }

        [Required(ErrorMessage = "Comment is required")]
        [StringLength(1000, ErrorMessage = "Comment cannot be longer than 1000 characters")]
        public string Content { get; set; }
    }
}
