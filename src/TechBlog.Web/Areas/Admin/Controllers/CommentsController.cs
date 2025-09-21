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
    public class CommentsController : Controller
    {
        private readonly ICommentService _commentService;
        private readonly IBlogService _blogService;
        private readonly IMapper _mapper;

        public CommentsController(
            ICommentService commentService,
            IBlogService blogService,
            IMapper mapper)
        {
            _commentService = commentService;
            _blogService = blogService;
            _mapper = mapper;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index(int? page = 1, string? status = null, string? search = null, string? sortBy = null, int pageSize = 10)
        {
            // These ViewBag keys are used by the view
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;
            ViewBag.SortBy = sortBy;

            // Include both approved and pending comments for admin moderation
            var allComments = await _commentService.GetAllCommentsAsync(includeUnapproved: true);
            var comments = allComments;
            
            if (!string.IsNullOrEmpty(status))
            {
                comments = status.ToLower() == "approved" 
                    ? comments.Where(c => c.IsApproved)
                    : comments.Where(c => !c.IsApproved);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                comments = comments.Where(c => 
                    c.Content.ToLower().Contains(search) || 
                    c.AuthorName.ToLower().Contains(search) ||
                    c.AuthorEmail.ToLower().Contains(search));
            }

            // Sorting
            var mapped = _mapper.Map<IEnumerable<CommentAdminListDto>>(comments);
            mapped = (sortBy ?? "newest").ToLower() switch
            {
                "oldest" => mapped.OrderBy(c => c.CreatedAt),
                "post" => mapped.OrderBy(c => c.BlogPostTitle).ThenByDescending(c => c.CreatedAt),
                _ => mapped.OrderByDescending(c => c.CreatedAt)
            };

            var paged = mapped.ToPagedList(page ?? 1, pageSize);

            var model = new CommentListViewModel
            {
                Comments = paged,
                TotalCount = allComments.Count(),
                ApprovedCount = allComments.Count(c => c.IsApproved),
                PendingCount = allComments.Count(c => !c.IsApproved),
                CurrentPage = paged.PageIndex,
                TotalPages = paged.TotalPages
            };
            ViewBag.PageSize = pageSize;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            await _commentService.ApproveCommentAsync(id);
            
            TempData["Success"] = "Comment approved successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAction(string action, int[] commentIds)
        {
            if (commentIds == null || commentIds.Length == 0)
            {
                TempData["Error"] = "No comments selected.";
                return RedirectToAction(nameof(Index));
            }

            int processed = 0;
            foreach (var id in commentIds)
            {
                try
                {
                    switch ((action ?? string.Empty).ToLower())
                    {
                        case "approve":
                            await _commentService.ApproveCommentAsync(id);
                            processed++;
                            break;
                        case "unapprove":
                            await _commentService.UnapproveCommentAsync(id);
                            processed++;
                            break;
                        case "delete":
                            await _commentService.DeleteCommentAsync(id);
                            processed++;
                            break;
                    }
                }
                catch
                {
                    // Ignore individual failures; continue processing
                }
            }

            if (processed > 0)
            {
                TempData["Success"] = $"Successfully processed {processed} comment(s).";
            }
            else
            {
                TempData["Error"] = "No comments were processed.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unapprove(int id)
        {
            await _commentService.UnapproveCommentAsync(id);
            
            TempData["Success"] = "Comment unapproved successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var comment = await _commentService.GetCommentByIdAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            await _commentService.DeleteCommentAsync(id);
            
            TempData["Success"] = "Comment deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple(int[] commentIds)
        {
            if (commentIds == null || commentIds.Length == 0)
            {
                TempData["Error"] = "No comments selected for deletion.";
                return RedirectToAction(nameof(Index));
            }

            int deletedCount = 0;
            foreach (var id in commentIds)
            {
                try
                {
                    await _commentService.DeleteCommentAsync(id);
                    deletedCount++;
                }
                catch
                {
                    // Log error but continue with other deletions
                }
            }

            if (deletedCount > 0)
            {
                TempData["Success"] = $"Successfully deleted {deletedCount} comment(s).";
            }
            else
            {
                TempData["Error"] = "No comments were deleted. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
