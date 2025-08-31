using System.Collections.Generic;
using TechBlog.Core.DTOs;
using TechBlog.Web.Extensions;

namespace TechBlog.Web.Areas.Admin.Models
{
    public class CommentListViewModel
    {
        public IPagedList<CommentAdminListDto> Comments { get; set; } = new PagedList<CommentAdminListDto>(new List<CommentAdminListDto>(), 0, 1, 1);
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
