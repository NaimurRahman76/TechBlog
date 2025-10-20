using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using TechBlog.Core.Interfaces.Services;

namespace TechBlog.Web.Filters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ValidateRecaptchaAttribute : TypeFilterAttribute
    {
        public ValidateRecaptchaAttribute(string action) : base(typeof(ValidateRecaptchaFilter))
        {
            Arguments = new object[] { action };
        }
    }

    public class ValidateRecaptchaFilter : IAsyncActionFilter
    {
        private readonly string _action;
        private readonly IServiceProvider _serviceProvider;

        public ValidateRecaptchaFilter(string action, IServiceProvider serviceProvider)
        {
            _action = action;
            _serviceProvider = serviceProvider;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            using var scope = _serviceProvider.CreateScope();
            var recaptchaService = scope.ServiceProvider.GetRequiredService<IRecaptchaService>();
            
            var settings = await recaptchaService.GetSettingsAsync();
            if (!settings.IsEnabled)
            {
                await next();
                return;
            }

            if (!context.HttpContext.Request.HasFormContentType || 
                !context.HttpContext.Request.Form.TryGetValue("g-recaptcha-response", out var token) || 
                string.IsNullOrEmpty(token))
            {
                context.Result = new BadRequestObjectResult(new { Error = "reCAPTCHA validation failed. Please try again." });
                return;
            }

            var isValid = await recaptchaService.VerifyCaptchaAsync(token, _action);
            if (!isValid)
            {
                context.Result = new BadRequestObjectResult(new { Error = "reCAPTCHA validation failed. Please try again." });
                return;
            }

            await next();
        }
    }
}
