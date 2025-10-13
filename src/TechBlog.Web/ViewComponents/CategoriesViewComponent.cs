using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using TechBlog.Core.Interfaces;
using TechBlog.Core.DTOs;

namespace TechBlog.Web.ViewComponents
{
    [ViewComponent(Name = "Categories")]
    public class CategoriesViewComponent : ViewComponent
    {
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;

        public CategoriesViewComponent(ICategoryService categoryService, IMapper mapper)
        {
            _categoryService = categoryService;
            _mapper = mapper;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);
                return View(categoryDtos);
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to inject ILogger in the constructor)
                // For now, we'll return an empty list
                return View(new List<Core.DTOs.CategoryDto>());
            }
        }
    }
}
