using Microsoft.AspNetCore.Mvc;
using TechBlog.Core.Interfaces;
using TechBlog.Web.Models;
using AutoMapper;
using TechBlog.Core.Entities;
using TechBlog.Core.DTOs;
using TechBlog.Web.Models;

namespace TechBlog.Web.Controllers
{
    public class BlogController : Controller
    {
        private readonly IBlogService _blogService;
        private readonly ICategoryService _categoryService;
        private readonly ITagService _tagService;
        private readonly ICommentService _commentService;
        private readonly IMapper _mapper;

        public BlogController(IBlogService blogService, ICategoryService categoryService, ITagService tagService, ICommentService commentService, IMapper mapper)
        {
            _blogService = blogService;
            _categoryService = categoryService;
            _tagService = tagService;
            _commentService = commentService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index(int page = 1, string search = null)
        {
            const int pageSize = 10;
            
            var posts = string.IsNullOrEmpty(search) 
                ? await _blogService.GetAllPostsAsync()
                : await _blogService.SearchPostsAsync(search);
            
            var postDtos = _mapper.Map<IEnumerable<PostListDto>>(posts);
            
            var model = new BlogIndexViewModel
            {
                Posts = postDtos.Skip((page - 1) * pageSize).Take(pageSize),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(postDtos.Count() / (double)pageSize),
                SearchTerm = search
            };
            
            return View(model);
        }

        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            var post = await _blogService.GetPostBySlugAsync(slug);
            if (post == null || !post.IsPublished)
            {
                return NotFound();
            }

            // Increment view count
            await _blogService.IncrementViewCountAsync(post.Id);

            var relatedPosts = await _blogService.GetRelatedPostsAsync(post.Id, 3);
            
            var model = new BlogPostViewModel
            {
                Post = _mapper.Map<PostDetailDto>(post),
                RelatedPosts = _mapper.Map<IEnumerable<PostListDto>>(relatedPosts)
            };

            return View(model);
        }

        public async Task<IActionResult> Category(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            var category = await _categoryService.GetCategoryBySlugAsync(slug);
            if (category == null)
            {
                return NotFound();
            }

            var posts = await _blogService.GetPostsByCategoryAsync(category.Id);
            
            var model = new BlogCategoryViewModel
            {
                Category = _mapper.Map<CategoryDto>(category),
                Posts = _mapper.Map<IEnumerable<PostListDto>>(posts)
            };

            return View(model);
        }

        public async Task<IActionResult> Tag(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            var tag = await _tagService.GetTagBySlugAsync(slug);
            if (tag == null)
            {
                return NotFound();
            }

            var posts = await _blogService.GetPostsByTagAsync(tag.Id);
            
            var model = new BlogTagViewModel
            {
                Tag = _mapper.Map<TagDto>(tag),
                Posts = _mapper.Map<IEnumerable<PostListDto>>(posts)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int postId, string authorName, string authorEmail, string content)
        {
            if (string.IsNullOrEmpty(authorName) || string.IsNullOrEmpty(authorEmail) || string.IsNullOrEmpty(content))
            {
                return Json(new { success = false, message = "All fields are required." });
            }

            var post = await _blogService.GetPostByIdAsync(postId);
            if (post == null)
            {
                return Json(new { success = false, message = "Post not found." });
            }

            var comment = new Comment
            {
                BlogPostId = postId,
                AuthorName = authorName,
                AuthorEmail = authorEmail,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsApproved = true // Auto-approve for now
            };

            try
            {
                await _commentService.CreateCommentAsync(comment);
                return Json(new { success = true, message = "Comment added successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to add comment. Please try again." });
            }
        }
    }
}
