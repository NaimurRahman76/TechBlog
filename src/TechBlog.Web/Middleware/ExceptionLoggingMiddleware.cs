using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TechBlog.Core.Interfaces;

namespace TechBlog.Web.Middleware
{
    public class ExceptionLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ILoggingService loggingService)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                try
                {
                    var userId = context.User?.Identity?.IsAuthenticated == true
                        ? context.User.FindFirst("sub")?.Value ?? context.User.Identity?.Name
                        : null;
                    var ip = context.Connection?.RemoteIpAddress?.ToString();
                    var ua = context.Request?.Headers["User-Agent"].ToString();

                    await loggingService.AddErrorAsync(
                        message: $"Unhandled exception for {context.Request?.Method} {context.Request?.Path}",
                        source: nameof(ExceptionLoggingMiddleware),
                        userId: userId,
                        ip: ip,
                        userAgent: ua,
                        ex: ex);
                }
                catch
                {
                    // Swallow logging errors
                }

                throw; // rethrow to preserve the default error handling/DeveloperExceptionPage
            }
        }
    }
}
