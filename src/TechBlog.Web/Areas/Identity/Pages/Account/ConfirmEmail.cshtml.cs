using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using TechBlog.Core.Entities;

namespace TechBlog.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ConfirmEmailModel> _logger;

        public ConfirmEmailModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ConfirmEmailModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code, string returnUrl = null)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Unable to find user with ID '{UserId}' for email confirmation", userId);
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await _userManager.ConfirmEmailAsync(user, code);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} confirmed their email successfully", user.Email);
                    StatusMessage = "Thank you for confirming your email. You can now log in.";
                    
                    // Optionally auto-sign in the user after email confirmation
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return LocalRedirect(returnUrl);
                    }
                    
                    return Page();
                }
                else
                {
                    _logger.LogWarning("Email confirmation failed for user {Email}: {Errors}", 
                        user.Email, string.Join(", ", result.Errors));
                    StatusMessage = "Error confirming your email. The link may have expired or is invalid.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email for user {Email}", user.Email);
                StatusMessage = "An error occurred while confirming your email. Please try again or contact support.";
                return Page();
            }
        }
    }
}
