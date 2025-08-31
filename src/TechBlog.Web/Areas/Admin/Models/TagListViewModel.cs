using System.Collections.Generic;
using TechBlog.Core.DTOs;
using TechBlog.Web.Extensions;

namespace TechBlog.Web.Areas.Admin.Models
{
    public class TagListViewModel
    {
        public IPagedList<TagDto> Tags { get; set; } = new PagedList<TagDto>(new List<TagDto>(), 0, 1, 1);
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
