using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces.Services;

namespace TechBlog.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            
            // Don't reveal that the user does not exist or is not confirmed
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                _logger.LogWarning("Password reset attempted for non-existent or unconfirmed email: {Email}", Input.Email);
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            try
            {
                // Generate password reset token
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                var emailSent = await _emailService.SendPasswordResetAsync(
                    Input.Email,
                    $"{user.FirstName} {user.LastName}",
                    callbackUrl);

                if (emailSent)
                {
                    _logger.LogInformation("Password reset email sent to {Email}", Input.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to send password reset email to {Email}", Input.Email);
                }

                // Always redirect to confirmation page to prevent user enumeration
                return RedirectToPage("./ForgotPasswordConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", Input.Email);
                // Still redirect to confirmation page to prevent user enumeration
                return RedirectToPage("./ForgotPasswordConfirmation");
            }
        }
    }
}
