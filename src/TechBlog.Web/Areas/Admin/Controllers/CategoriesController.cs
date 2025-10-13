using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechBlog.Core.DTOs;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;
using TechBlog.Web.Areas.Admin.Models;
using TechBlog.Web.Extensions;

namespace TechBlog.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;

        public CategoriesController(ICategoryService categoryService, IMapper mapper)
        {
            _categoryService = categoryService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index(int? page = 1, string? search = null, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = search;
            ViewBag.CurrentSearch = search;

            var categories = await _categoryService.GetAllCategoriesAsync();
            
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                categories = categories.Where(c => c.Name.ToLower().Contains(search) || 
                                                c.Description.ToLower().Contains(search));
            }

            var paged = _mapper.Map<IEnumerable<CategoryDto>>(categories)
                .OrderBy(c => c.Name)
                .ToPagedList(page ?? 1, pageSize);

            var model = new CategoryListViewModel
            {
                Categories = paged,
                TotalCount = categories.Count(),
                CurrentPage = paged.PageIndex,
                TotalPages = paged.TotalPages
            };
            ViewBag.PageSize = pageSize;
            return View(model);
        }

        public IActionResult Create()
        {
            return View(new CreateCategoryDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCategoryDto model)
        {
            if (ModelState.IsValid)
            {
                var category = _mapper.Map<Category>(model);
                var result = await _categoryService.CreateCategoryAsync(category);
                
                if (result != null)
                {
                    TempData["Success"] = "Category created successfully!";
                    return RedirectToAction(nameof(Edit), new { id = result.Id });
                }
                
                ModelState.AddModelError("", "An error occurred while creating the category.");
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var model = _mapper.Map<UpdateCategoryDto>(category);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateCategoryDto model)
        {
            if (ModelState.IsValid)
            {
                var category = await _categoryService.GetCategoryByIdAsync(model.Id);
                if (category == null)
                {
                    return NotFound();
                }

                _mapper.Map(model, category);
                await _categoryService.UpdateCategoryAsync(category);
                
                TempData["Success"] = "Category updated successfully!";
                return RedirectToAction(nameof(Edit), new { id = model.Id });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            try
            {
                await _categoryService.DeleteCategoryAsync(id);
                TempData["Success"] = "Category deleted successfully!";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }
            
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var category = await _categoryService.GetCategoryByIdWithPostsAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var model = new CategoryDetailDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Slug = category.Slug,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                PostsCount = category.BlogPosts?.Count ?? 0,
                RecentPosts = category.BlogPosts?
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .Select(p => new PostListItemDto
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Slug = p.Slug,
                        CreatedAt = p.CreatedAt,
                        IsPublished = p.IsPublished
                    })
                    .ToList() ?? new List<PostListItemDto>()
            };
                
            return View(model);
        }
    }
}
