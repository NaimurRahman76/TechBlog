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
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<ResendEmailConfirmationModel> _logger;

        public ResendEmailConfirmationModel(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<ResendEmailConfirmationModel> logger)
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
            if (user == null)
            {
                // Don't reveal that the user does not exist
                _logger.LogWarning("Resend email confirmation attempted for non-existent email: {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "Verification email sent. Please check your email.");
                return Page();
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                // Email is already confirmed
                _logger.LogInformation("Resend email confirmation attempted for already confirmed email: {Email}", Input.Email);
                TempData["InfoMessage"] = "Your email is already confirmed. You can log in.";
                return RedirectToPage("./Login");
            }

            try
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = code },
                    protocol: Request.Scheme);

                var emailSent = await _emailService.SendEmailVerificationAsync(
                    Input.Email,
                    $"{user.FirstName} {user.LastName}",
                    callbackUrl);

                if (emailSent)
                {
                    _logger.LogInformation("Verification email resent to {Email}", Input.Email);
                    TempData["SuccessMessage"] = "Verification email sent. Please check your email.";
                    return RedirectToPage("./RegisterConfirmation", new { email = Input.Email });
                }
                else
                {
                    _logger.LogError("Failed to resend verification email to {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Failed to send verification email. Please try again later or contact support.");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email to {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "An error occurred while sending the verification email. Please try again later.");
                return Page();
            }
        }
    }
}
