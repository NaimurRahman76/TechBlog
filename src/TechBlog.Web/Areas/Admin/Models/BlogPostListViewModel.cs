using System.Collections.Generic;
using TechBlog.Core.DTOs;
using TechBlog.Web.Extensions;

namespace TechBlog.Web.Areas.Admin.Models
{
    public class BlogPostListViewModel
    {
        public IPagedList<PostAdminListDto> BlogPosts { get; set; } = new PagedList<PostAdminListDto>(new List<PostAdminListDto>(), 0, 1, 1);
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
