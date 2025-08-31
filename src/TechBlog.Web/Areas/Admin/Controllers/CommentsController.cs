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

        public async Task<IActionResult> Index(int? page = 1, string? status = null, string? search = null)
        {
            ViewData["StatusFilter"] = status;
            ViewData["CurrentFilter"] = search;

            var comments = await _commentService.GetAllCommentsAsync();
            
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

            var model = new CommentListViewModel
            {
                Comments = _mapper.Map<IEnumerable<CommentAdminListDto>>(comments)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToPagedList(page ?? 1, 20),
                TotalCount = comments.Count()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var comment = await _commentService.GetCommentByIdAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            comment.IsApproved = true;
            await _commentService.UpdateCommentAsync(comment);
            
            TempData["Success"] = "Comment approved successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unapprove(int id)
        {
            var comment = await _commentService.GetCommentByIdAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            comment.IsApproved = false;
            await _commentService.UpdateCommentAsync(comment);
            
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
