using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechBlog.Core.Interfaces;

namespace TechBlog.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LogsController : Controller
    {
        private readonly ILoggingService _loggingService;

        public LogsController(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? level = null, string? search = null, int page = 1, int pageSize = 50,
            DateTime? fromUtc = null, DateTime? toUtc = null, string? source = null, string? userId = null)
        {
            ViewBag.Level = level;
            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.FromUtc = fromUtc?.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.ToUtc = toUtc?.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.Source = source;
            ViewBag.UserId = userId;

            var logs = await _loggingService.GetLogsAsync(level, search, page, pageSize, fromUtc, toUtc, source, userId);
            var total = await _loggingService.GetLogsCountAsync(level, search, fromUtc, toUtc, source, userId);
            ViewBag.Total = total;
            return View(logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear(string? level = null)
        {
            await _loggingService.ClearAsync(level);
            TempData["Success"] = level == null ? "All logs cleared." : $"Logs cleared for level '{level}'.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var log = await _loggingService.GetLogAsync(id);
            if (log == null)
            {
                return NotFound();
            }
            return View(log);
        }
    }
}
