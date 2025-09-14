using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechBlog.Core.Interfaces;

namespace TechBlog.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UtilitiesController : Controller
    {
        private readonly ICacheService _cacheService;
        private readonly ICommentService _commentService;

        public UtilitiesController(ICacheService cacheService, ICommentService commentService)
        {
            _cacheService = cacheService;
            _commentService = commentService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCache()
        {
            _cacheService.ClearAll();
            // Also clear known comment cache groups for extra safety
            _commentService.InvalidateAllCommentsCache();
            TempData["Success"] = "All caches cleared successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
