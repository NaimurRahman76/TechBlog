using System;
using System.Collections.Generic;
using TechBlog.Core.Interfaces;
using TechBlog.Core.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechBlog.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using TechBlog.Web.Extensions;
using TechBlog.Core.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TechBlog.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BlogPostsController : Controller
    {
        private readonly IBlogService _blogService;
        private readonly ICategoryService _categoryService;
        private readonly ITagService _tagService;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BlogPostsController(
            IBlogService blogService,
            ICategoryService categoryService,
            ITagService tagService,
            IMapper mapper,
            IWebHostEnvironment webHostEnvironment)
        {
            _blogService = blogService;
            _categoryService = categoryService;
            _tagService = tagService;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(int? page = 1, string? search = null, string? status = null, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = search;
            ViewData["StatusFilter"] = status;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = status;

            var posts = await _blogService.GetAllPostsAsync(includeUnpublished: true);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                posts = posts.Where(p => p.Title.ToLower().Contains(search) ||
                                       p.Content.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                posts = status.ToLower() == "published"
                    ? posts.Where(p => p.IsPublished)
                    : posts.Where(p => !p.IsPublished);
            }

            // Apply pagination to the original query
            var paged = posts
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .ToPagedList(page ?? 1, pageSize);

            // Map only the items for this page
            var pagedItems = _mapper.Map<IEnumerable<PostAdminListDto>>(paged);

            // Create new paged list with mapped items but preserve pagination metadata
            var pagedMappedItems = new TechBlog.Web.Extensions.PagedList<PostAdminListDto>(
                pagedItems.ToList(),
                paged.TotalCount,
                paged.PageIndex,
                paged.PageSize
            );

            var model = new BlogPostListViewModel
            {
                BlogPosts = pagedMappedItems,
                TotalCount = paged.TotalCount,
                CurrentPage = paged.PageIndex,
                TotalPages = paged.TotalPages
            };
            ViewBag.PageSize = pageSize;
            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            await PrepareViewBagForPostEdit();
            return View(new CreatePostDto { IsPublished = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePostDto model)
        {
            // Get uploaded file from Request.Form.Files
            var featuredImage = Request.Form.Files.FirstOrDefault(f => f.Name == "featuredImage");
            
            if (ModelState.IsValid)
            {
                // Validate CategoryId exists
                if (model.CategoryId <= 0)
                {
                    ModelState.AddModelError("CategoryId", "Please select a category.");
                    await PrepareViewBagForPostEdit();
                    return View(model);
                }

                var categoryExists = await _categoryService.GetCategoryByIdAsync(model.CategoryId);
                if (categoryExists == null)
                {
                    ModelState.AddModelError("CategoryId", "Selected category does not exist.");
                    await PrepareViewBagForPostEdit();
                    return View(model);
                }

                var authorId = User.GetUserId();
                if (string.IsNullOrEmpty(authorId))
                {
                    ModelState.AddModelError("", "Unable to retrieve user information.");
                    await PrepareViewBagForPostEdit();
                    return View(model);
                }

                // Check if the user exists in the database
                var userService = HttpContext.RequestServices.GetRequiredService<IUserService>();
                var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();

                // First try to find by email
                var currentUser = await userManager.FindByEmailAsync(User.GetUserEmail());
                if (currentUser == null)
                {
                    // Try to find by ID
                    currentUser = await userManager.FindByIdAsync(authorId);
                }

                if (currentUser == null)
                {
                    // Create the user if they don't exist
                    var newUser = new ApplicationUser
                    {
                        Id = authorId,
                        UserName = User.GetUserName(), // Use the actual username, not email
                        Email = User.GetUserEmail(),
                        FirstName = User.GetUserName() ?? "User",
                        LastName = "",
                        EmailConfirmed = true
                    };

                    var createResult = await userManager.CreateAsync(newUser);
                    if (!createResult.Succeeded)
                    {
                        ModelState.AddModelError("", $"Unable to create user account: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                        await PrepareViewBagForPostEdit();
                        return View(model);
                    }

                    // Add to User role by default
                    await userManager.AddToRoleAsync(newUser, "User");
                }

                // Handle image upload
                if (featuredImage != null && featuredImage.Length > 0)
                {
                    var imageUrl = await SaveImageAsync(featuredImage);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        model.FeaturedImageUrl = imageUrl;
                    }
                }

                // Generate slug if not provided
                if (string.IsNullOrEmpty(model.Slug))
                {
                    model.Slug = model.Title; // Will be processed by BlogService
                }

                var post = _mapper.Map<BlogPost>(model);
                post.AuthorId = authorId;
                var tagArray = string.IsNullOrWhiteSpace(model.Tags)
                    ? Array.Empty<string>()
                    : model.Tags
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToArray();

                var result = await _blogService.CreatePostAsync(post, tagArray);
                
                if (result != null)
                {
                    TempData["Success"] = "Blog post created successfully!";
                    return RedirectToAction(nameof(Edit), new { id = result.Id });
                }
                
                ModelState.AddModelError("", "An error occurred while creating the blog post.");
            }

            await PrepareViewBagForPostEdit();
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var post = await _blogService.GetPostByIdAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var model = _mapper.Map<UpdatePostDto>(post);
            model.Tags = string.Join(",", post.BlogPostTags?.Select(t => t.Tag.Name) ?? new List<string>());
            
            // Pass the existing image URL to the view for display
            ViewBag.ExistingImageUrl = post.FeaturedImageUrl;
            
            await PrepareViewBagForPostEdit();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdatePostDto model)
        {
            // Get uploaded file from Request.Form.Files
            var featuredImage = Request.Form.Files.FirstOrDefault(f => f.Name == "featuredImage");
            
            // Debug logging
            System.Diagnostics.Debug.WriteLine($"Edit POST called with image: {featuredImage?.FileName ?? "null"}");
            
            if (ModelState.IsValid)
            {
                var post = await _blogService.GetPostByIdAsync(model.Id);
                if (post == null)
                {
                    return NotFound();
                }

                // Handle image upload
                if (featuredImage != null && featuredImage.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Processing image upload: {featuredImage.FileName}, Size: {featuredImage.Length}");
                    var imageUrl = await SaveImageAsync(featuredImage);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        model.FeaturedImageUrl = imageUrl;
                        System.Diagnostics.Debug.WriteLine($"Image saved to: {imageUrl}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No image file received");
                    // Keep existing image if no new image uploaded
                    model.FeaturedImageUrl = post.FeaturedImageUrl;
                }

                // Ensure slug is preserved from existing post if not provided
                if (string.IsNullOrEmpty(model.Slug))
                {
                    model.Slug = post.Slug; // Keep existing slug if not provided
                }
                
                // Debug slug value
                System.Diagnostics.Debug.WriteLine($"Model slug: '{model.Slug}', Post slug: '{post.Slug}'");

                _mapper.Map(model, post);
                var tags = string.IsNullOrEmpty(model.Tags) ? Array.Empty<string>() : model.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
                await _blogService.UpdatePostAsync(post, tags);
                
                TempData["Success"] = "Blog post updated successfully!";
                return RedirectToAction(nameof(Edit), new { id = model.Id });
            }

            await PrepareViewBagForPostEdit();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _blogService.GetPostByIdAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            await _blogService.DeletePostAsync(id);
            
            TempData["Success"] = "Blog post deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var post = await _blogService.GetPostByIdAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            post.IsPublished = !post.IsPublished;
            post.PublishedAt = post.IsPublished ? DateTime.UtcNow : null;
            
            await _blogService.UpdatePostAsync(post, post.BlogPostTags?.Select(t => t.Tag.Name).ToArray() ?? Array.Empty<string>());
            
            TempData["Success"] = $"Blog post {(post.IsPublished ? "published" : "unpublished")} successfully!";
            return RedirectToAction(nameof(Edit), new { id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var post = await _blogService.GetPostByIdAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var model = _mapper.Map<PostDetailDto>(post);
            return View(model);
        }

        private async Task PrepareViewBagForPostEdit()
        {
            ViewBag.Categories = new SelectList(await _categoryService.GetAllCategoriesAsync(), "Id", "Name");
        }

        private async Task<string?> SaveImageAsync(IFormFile imageFile)
        {
            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "images");
                Directory.CreateDirectory(uploadsPath);

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                System.Diagnostics.Debug.WriteLine($"Saving image to: {filePath}");

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                var imageUrl = $"/uploads/images/{fileName}";
                System.Diagnostics.Debug.WriteLine($"Image URL: {imageUrl}");
                
                // Return relative URL
                return imageUrl;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving image: {ex.Message}");
                return null;
            }
        }
    }
}
