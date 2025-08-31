using System.Collections.Generic;
using TechBlog.Core.DTOs;
using TechBlog.Web.Extensions;

namespace TechBlog.Web.Areas.Admin.Models
{
    public class CategoryListViewModel
    {
        public IPagedList<CategoryDto> Categories { get; set; } = new PagedList<CategoryDto>(new List<CategoryDto>(), 0, 1, 1);
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
