using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;

namespace TechBlog.Infrastructure.Services
{
    public class WorkContext : IWorkContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private ApplicationUser _cachedUser;
        private bool _userInitialized = false;

        public WorkContext(
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public HttpContext HttpContext => _httpContextAccessor.HttpContext;

        public bool IsAuthenticated => HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public bool IsAdmin => IsInRole("Admin");

        public bool IsAuthor => IsInRole("Author") || IsAdmin;

        public string UserId => IsAuthenticated ? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value : null;

        public string UserEmail => IsAuthenticated ? HttpContext.User.FindFirst(ClaimTypes.Email)?.Value : null;

        public string UserName => IsAuthenticated ? HttpContext.User.Identity.Name : null;

        public ApplicationUser CurrentUser 
        {
            get 
            {
                if (!_userInitialized)
                {
                    _cachedUser = GetCurrentUserAsync().GetAwaiter().GetResult();
                    _userInitialized = true;
                }
                return _cachedUser;
            }
        }

        public async Task<ApplicationUser> GetCurrentUserAsync()
        {
            if (_cachedUser != null)
                return _cachedUser;

            if (!IsAuthenticated)
                return null;

            _cachedUser = await _userManager.GetUserAsync(HttpContext.User);
            _userInitialized = true;
            return _cachedUser;
        }

        private bool IsInRole(string role)
        {
            return IsAuthenticated && HttpContext.User.IsInRole(role);
        }
    }
}
