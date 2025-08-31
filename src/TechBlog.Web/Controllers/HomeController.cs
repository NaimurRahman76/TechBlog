using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TechBlog.Web.Models;
using TechBlog.Core.Interfaces;
using AutoMapper;
using TechBlog.Core.DTOs;

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
        var recentPosts = await _blogService.GetRecentPostsAsync(6);
        var categories = await _categoryService.GetAllCategoriesAsync();
        
        var model = new HomeViewModel
        {
            RecentPosts = _mapper.Map<IEnumerable<PostListDto>>(recentPosts),
            Categories = _mapper.Map<IEnumerable<CategoryDto>>(categories)
        };
        
        return View(model);
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
