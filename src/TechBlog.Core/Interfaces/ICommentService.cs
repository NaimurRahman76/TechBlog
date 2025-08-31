using System.Collections.Generic;
using System.Threading.Tasks;
using TechBlog.Core.Entities;

namespace TechBlog.Core.Interfaces
{
    public interface ICommentService
    {
        Task<IEnumerable<Comment>> GetAllCommentsAsync();
        Task<Comment> GetCommentByIdAsync(int id);
        Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(int postId);
        Task<IEnumerable<Comment>> GetCommentsByAuthorAsync(string authorId);
        Task<Comment> CreateCommentAsync(Comment comment);
        Task UpdateCommentAsync(Comment comment);
        Task DeleteCommentAsync(int id);
        Task<bool> CommentExistsAsync(int id);
        Task ApproveCommentAsync(int id);
        Task UnapproveCommentAsync(int id);
        Task<int> GetCommentsCountAsync(bool onlyApproved = true);
    }
}
