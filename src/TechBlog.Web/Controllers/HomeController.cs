using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TechBlog.Web.Models;
using TechBlog.Core.Interfaces;
using AutoMapper;
using TechBlog.Core.DTOs;
using TechBlog.Core.Entities;

namespace TechBlog.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IBlogService _blogService;
    private readonly ICategoryService _categoryService;
    private readonly IMapper _mapper;

    public HomeController(ILogger<HomeController> logger, IBlogService blogService, ICategoryService categoryService, IMapper mapper)
    {
        _logger = logger;
        _blogService = blogService;
        _categoryService = categoryService;
        _mapper = mapper;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            _logger.LogInformation("Fetching recent posts...");
            var recentPosts = await _blogService.GetRecentPostsAsync(6);
            _logger.LogInformation($"Fetched {recentPosts?.Count() ?? 0} recent posts");
            
            _logger.LogInformation("Fetching all categories...");
            var categories = await _categoryService.GetAllCategoriesAsync();
            _logger.LogInformation($"Fetched {categories?.Count() ?? 0} categories");
            
            _logger.LogInformation("Mapping to view models...");
            var model = new HomeViewModel
            {
                RecentPosts = _mapper.Map<IEnumerable<PostListDto>>(recentPosts ?? Enumerable.Empty<Core.Entities.BlogPost>()),
                Categories = _mapper.Map<IEnumerable<CategoryDto>>(categories ?? Enumerable.Empty<Core.Entities.Category>())
            };
            
            _logger.LogInformation("Rendering Index view");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Home/Index: {Message}", ex.Message);
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
            }
            throw; // Re-throw to let the error handling middleware handle it
        }
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
