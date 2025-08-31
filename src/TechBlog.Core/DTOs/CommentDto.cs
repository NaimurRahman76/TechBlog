using System;
using System.Collections.Generic;

namespace TechBlog.Core.DTOs
{
    public class CommentDto : BaseDto
    {
        public string Content { get; set; }
        public string AuthorName { get; set; }
        public string AuthorEmail { get; set; }
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public int? ParentCommentId { get; set; }
        public int PostId { get; set; }
        public int BlogPostId { get; set; }
        public string PostTitle { get; set; }
        public string PostSlug { get; set; }
        public string BlogPostTitle { get; set; }
        public string BlogPostSlug { get; set; }
        public ICollection<CommentDto> Replies { get; set; } = new List<CommentDto>();
    }

    public class CreateCommentDto
    {
        public string Content { get; set; }
        public string AuthorName { get; set; }
        public string AuthorEmail { get; set; }
        public int? ParentCommentId { get; set; }
        public int PostId { get; set; }
    }

    public class UpdateCommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public bool IsApproved { get; set; }
    }

    public class CommentAdminListDto : CommentDto
    {
        public string BlogPostTitle { get; set; }
        public string BlogPostSlug { get; set; }
        public int BlogPostId { get; set; }
        public bool IsRejected { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public int ReplyCount { get; set; }
    }
}
