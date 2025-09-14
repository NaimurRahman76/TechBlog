using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TechBlog.Core.DTOs;
using TechBlog.Core.Entities;
using TechBlog.Core.Exceptions;
using TechBlog.Core.Interfaces;
using TechBlog.Web.Models;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace TechBlog.Web.Controllers
{
    public class BlogController : Controller
    {
        private readonly IBlogService _blogService;
        private readonly ICategoryService _categoryService;
        private readonly ITagService _tagService;
        private readonly ICommentService _commentService;
        private readonly IWorkContext _workContext;
        private readonly IMapper _mapper;
        private readonly ILogger<BlogController> _logger;

        public BlogController(
            IBlogService blogService,
            ICategoryService categoryService,
            ITagService tagService,
            ICommentService commentService,
            IWorkContext workContext,
            IMapper mapper,
            ILogger<BlogController> logger)
        {
            _blogService = blogService ?? throw new ArgumentNullException(nameof(blogService));
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
            _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
            _workContext = workContext ?? throw new ArgumentNullException(nameof(workContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> CommentsPartial(int postId)
        {
            try
            {
                var comments = await LoadCommentsForPost(postId);
                return PartialView("_CommentThread", comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load comments partial for post {PostId}", postId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<IEnumerable<CommentDto>> LoadCommentsForPost(int postId)
        {
            IEnumerable<Comment> commentsToShow;
            if (_workContext.IsAdmin || _workContext.IsAuthor)
            {
                commentsToShow = await _commentService.GetCommentsByPostIdAsync(postId, true);
            }
            else
            {
                var approved = await _commentService.GetCommentsByPostIdAsync(postId, false);
                if (_workContext.IsAuthenticated)
                {
                    var all = await _commentService.GetCommentsByPostIdAsync(postId, true);
                    var currentUser = await _workContext.GetCurrentUserAsync();
                    var minePending = all.Where(c => !c.IsApproved && c.AuthorId == currentUser?.Id);
                    commentsToShow = approved.Concat(minePending).OrderByDescending(c => c.CreatedAt);
                }
                else
                {
                    commentsToShow = approved;
                }
            }

            // Build a threaded tree so replies render under their parents
            var byId = commentsToShow.ToDictionary(c => c.Id);
            var roots = new List<Comment>();
            foreach (var c in commentsToShow)
            {
                if (c.ParentCommentId.HasValue && byId.ContainsKey(c.ParentCommentId.Value))
                {
                    // Ensure Replies collection exists on parent to maintain tree in memory
                    byId[c.ParentCommentId.Value].Replies ??= new List<Comment>();
                    byId[c.ParentCommentId.Value].Replies.Add(c);
                }
                else
                {
                    roots.Add(c);
                }
            }

            // Map recursively to DTOs
            IEnumerable<CommentDto> MapTree(IEnumerable<Comment> nodes)
            {
                foreach (var n in nodes.OrderByDescending(x => x.CreatedAt))
                {
                    var dto = new CommentDto
                    {
                        Id = n.Id,
                        Content = n.Content,
                        AuthorName = n.AuthorName,
                        AuthorEmail = n.AuthorEmail,
                        IsApproved = n.IsApproved,
                        ParentCommentId = n.ParentCommentId,
                        BlogPostId = n.BlogPostId,
                        CreatedAt = n.CreatedAt,
                        UpdatedAt = n.UpdatedAt
                    };
                    if (n.Replies != null && n.Replies.Any())
                    {
                        dto.Replies = MapTree(n.Replies).ToList();
                    }
                    yield return dto;
                }
            }

            return MapTree(roots).ToList();
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

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
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

            // Load comments
            try
            {
                var commentsDto = await LoadCommentsForPost(post.Id);
                model.Post.Comments = commentsDto.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load comments for post {PostId}", post.Id);
            }

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(AddCommentViewModel model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new { success = false, message = "Invalid request data." });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new { success = false, message = "Validation failed.", errors });
                }

                var post = await _blogService.GetPostByIdAsync(model.PostId);
                if (post == null)
                {
                    _logger.LogWarning("Post with ID {PostId} not found when adding comment", model.PostId);
                    return NotFound(new { success = false, message = "Post not found." });
                }

                var comment = new Comment
                {
                    BlogPostId = model.PostId,
                    ParentCommentId = model.ParentCommentId,
                    Content = model.Content,
                    CreatedAt = DateTime.UtcNow,
                    IsApproved = _workContext.IsAdmin || _workContext.IsAuthor // Auto-approve for admins/authors
                };

                // Set author information
                if (_workContext.IsAuthenticated)
                {
                    var currentUser = await _workContext.GetCurrentUserAsync();
                    if (currentUser != null)
                    {
                        comment.AuthorId = currentUser.Id;
                        // Respect edited values; if empty, fall back to account info
                        comment.AuthorName = string.IsNullOrWhiteSpace(model.AuthorName)
                            ? currentUser.UserName
                            : model.AuthorName;
                        comment.AuthorEmail = string.IsNullOrWhiteSpace(model.AuthorEmail)
                            ? currentUser.Email
                            : model.AuthorEmail;
                    }
                }
                else
                {
                    // For anonymous users, use the provided information
                    comment.AuthorName = model.AuthorName;
                    comment.AuthorEmail = model.AuthorEmail;
                }

                var createdComment = await _commentService.CreateCommentAsync(comment);
                
                _logger.LogInformation("New comment added by {AuthorName} on post {PostId}", 
                    comment.AuthorName, model.PostId);
                
                // Return the new comment data for dynamic update
                return Ok(new 
                { 
                    success = true, 
                    message = _workContext.IsAdmin || _workContext.IsAuthor 
                        ? "Comment added successfully!" 
                        : "Your comment has been submitted and is awaiting moderation.",
                    comment = new 
                    {
                        id = createdComment.Id,
                        authorName = createdComment.AuthorName,
                        content = createdComment.Content,
                        createdAt = createdComment.CreatedAt.ToString("MMMM dd, yyyy"),
                        parentCommentId = createdComment.ParentCommentId,
                        isAdmin = _workContext.IsAdmin,
                        isAuthor = _workContext.IsAuthor,
                        isApproved = createdComment.IsApproved
                    }
                });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error when adding comment");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment for post {PostId}", model?.PostId);
                return StatusCode(StatusCodes.Status500InternalServerError, new 
                { 
                    success = false, 
                    message = "An unexpected error occurred while adding your comment. Please try again later." 
                });
            }
        }
    }
}
