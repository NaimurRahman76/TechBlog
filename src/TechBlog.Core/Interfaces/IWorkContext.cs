using Microsoft.AspNetCore.Http;
using TechBlog.Core.Entities;

namespace TechBlog.Core.Interfaces
{
    public interface IWorkContext
    {
        ApplicationUser CurrentUser { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
        bool IsAuthor { get; }
        string UserId { get; }
        string UserEmail { get; }
        string UserName { get; }
        HttpContext HttpContext { get; }
        Task<ApplicationUser> GetCurrentUserAsync();
    }
}
