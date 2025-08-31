using System;
using System.Collections.Generic;
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
    public class TagsController : Controller
    {
        private readonly ITagService _tagService;
        private readonly IMapper _mapper;

        public TagsController(ITagService tagService, IMapper mapper)
        {
            _tagService = tagService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index(int? page = 1, string? search = null)
        {
            ViewData["CurrentFilter"] = search;

            var tags = await _tagService.GetAllTagsAsync();
            
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                tags = tags.Where(t => t.Name.ToLower().Contains(search));
            }

            var model = new TagListViewModel
            {
                Tags = _mapper.Map<IEnumerable<TagDto>>(tags)
                    .OrderBy(t => t.Name)
                    .ToPagedList(page ?? 1, 20),
                TotalCount = tags.Count()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Tag name is required.";
                return RedirectToAction(nameof(Index));
            }

            var tag = new Tag { Name = name.Trim() };
            var result = await _tagService.CreateTagAsync(tag);
            
            if (result != null)
            {
                TempData["Success"] = "Tag created successfully!";
            }
            else
            {
                TempData["Error"] = "An error occurred while creating the tag.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            if (tag == null)
            {
                return NotFound();
            }

            var model = _mapper.Map<UpdateTagDto>(tag);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateTagDto model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                var tag = await _tagService.GetTagByIdAsync(id);
                if (tag == null)
                {
                    return NotFound();
                }

                _mapper.Map(model, tag);

                await _tagService.UpdateTagAsync(tag);
                TempData["Success"] = "Tag updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            if (tag == null)
            {
                return NotFound();
            }

            try
            {
                await _tagService.DeleteTagAsync(id);
                TempData["Success"] = "Tag deleted successfully!";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
