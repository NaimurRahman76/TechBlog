using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechBlog.Core.Interfaces;
using TechBlog.Web.Areas.Admin.Models;
using AutoMapper;

namespace TechBlog.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly IBlogService _blogService;
        private readonly ICategoryService _categoryService;
        private readonly ITagService _tagService;
        private readonly IMapper _mapper;

        public DashboardController(
            IBlogService blogService,
            ICategoryService categoryService,
            ITagService tagService,
            IMapper mapper)
        {
            _blogService = blogService;
            _categoryService = categoryService;
            _tagService = tagService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            var recentPosts = await _blogService.GetRecentPostsAsync(5);
            
            var model = new DashboardViewModel
            {
                TotalPosts = await _blogService.GetTotalPostsCountAsync(),
                PublishedPosts = await _blogService.GetPublishedPostsCountAsync(),
                DraftPosts = await _blogService.GetDraftPostsCountAsync(),
                TotalCategories = await _categoryService.GetTotalCategoriesCountAsync(),
                TotalTags = await _tagService.GetTotalTagsCountAsync(),
                RecentPosts = _mapper.Map<IEnumerable<TechBlog.Core.DTOs.PostListDto>>(recentPosts),
                TotalViews = recentPosts.Sum(p => p.ViewCount),
                TotalComments = recentPosts.Sum(p => p.Comments?.Count ?? 0)
            };

            return View(model);
        }
    }
}
