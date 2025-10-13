using Microsoft.AspNetCore.Mvc;
using TechBlog.Core.Interfaces;

namespace TechBlog.Web.ViewComponents
{
    [ViewComponent(Name = "PopularTags")]
    public class PopularTagsViewComponent : ViewComponent
    {
        private readonly ITagService _tagService;

        public PopularTagsViewComponent(ITagService tagService)
        {
            _tagService = tagService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var tags = await _tagService.GetPopularTagsAsync(10); // Get top 10 popular tags
                return View(tags);
            }
            catch (Exception)
            {
                // Return empty list if there's an error
                return View(new List<Core.DTOs.TagDto>());
            }
        }
    }
}
