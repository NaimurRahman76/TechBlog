using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;
using TechBlog.Infrastructure.Data;

namespace TechBlog.Infrastructure.Services
{
    public class CommentService : BaseService, ICommentService
    {
        public CommentService(
            ApplicationDbContext context,
            ILogger<CommentService> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Comment>> GetAllCommentsAsync()
        {
            return await _context.Comments
                .Include(c => c.BlogPost)
                .Include(c => c.Author)
                .Include(c => c.ParentComment)
                .Include(c => c.Replies)
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Comment> GetCommentByIdAsync(int id)
        {
            return await _context.Comments
                .Include(c => c.BlogPost)
                .Include(c => c.Author)
                .Include(c => c.ParentComment)
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }

        public async Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(int postId)
        {
            return await _context.Comments
                .Include(c => c.Author)
                .Include(c => c.Replies)
                .Where(c => c.BlogPostId == postId && !c.IsDeleted && c.IsApproved)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Comment>> GetCommentsByAuthorAsync(string authorId)
        {
            return await _context.Comments
                .Include(c => c.BlogPost)
                .Where(c => c.AuthorId == authorId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Comment> CreateCommentAsync(Comment comment)
        {
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            comment.CreatedAt = DateTime.UtcNow;
            // Keep the IsApproved value from the input (auto-approve for public comments)

            _context.Comments.Add(comment);
            await SaveChangesAsync();
            
            return comment;
        }

        public async Task UpdateCommentAsync(Comment comment)
        {
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            var existingComment = await GetCommentByIdAsync(comment.Id);
            if (existingComment == null)
                throw new KeyNotFoundException($"Comment with ID {comment.Id} not found.");

            // Only update specific fields
            existingComment.Content = comment.Content;
            existingComment.IsApproved = comment.IsApproved;
            existingComment.UpdatedAt = DateTime.UtcNow;

            await SaveChangesAsync();
        }

        public async Task DeleteCommentAsync(int id)
        {
            var comment = await GetCommentByIdAsync(id);
            if (comment == null)
                throw new KeyNotFoundException($"Comment with ID {id} not found.");

            // Soft delete
            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.UtcNow;

            // Also soft delete all replies
            if (comment.Replies != null && comment.Replies.Any())
            {
                foreach (var reply in comment.Replies)
                {
                    reply.IsDeleted = true;
                    reply.UpdatedAt = DateTime.UtcNow;
                }
            }

            await SaveChangesAsync();
        }

        public async Task<bool> CommentExistsAsync(int id)
        {
            return await _context.Comments.AnyAsync(c => c.Id == id && !c.IsDeleted);
        }

        public async Task ApproveCommentAsync(int id)
        {
            var comment = await GetCommentByIdAsync(id);
            if (comment == null)
                throw new KeyNotFoundException($"Comment with ID {id} not found.");

            comment.IsApproved = true;
            comment.UpdatedAt = DateTime.UtcNow;
            await SaveChangesAsync();
        }

        public async Task UnapproveCommentAsync(int id)
        {
            var comment = await GetCommentByIdAsync(id);
            if (comment == null)
                throw new KeyNotFoundException($"Comment with ID {id} not found.");

            comment.IsApproved = false;
            comment.UpdatedAt = DateTime.UtcNow;
            await SaveChangesAsync();
        }

        public async Task<int> GetCommentsCountAsync(bool onlyApproved = true)
        {
            var query = _context.Comments.Where(c => !c.IsDeleted);
            
            if (onlyApproved)
            {
                query = query.Where(c => c.IsApproved);
            }

            return await query.CountAsync();
        }
    }
}
