using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace TechBlog.Web.Extensions
{
    public static class UserExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public static string? GetUserEmail(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst(ClaimTypes.Email)?.Value;
        }

        public static string? GetUserName(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            return principal.FindFirst(ClaimTypes.Name)?.Value;
        }

        public static bool IsInAnyRole(this ClaimsPrincipal principal, params string[] roles)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            if (roles == null || roles.Length == 0)
                return false;

            return roles.Any(principal.IsInRole);
        }
    }
}
