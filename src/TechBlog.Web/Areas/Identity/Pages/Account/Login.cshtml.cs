using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces.Services;
using TechBlog.Web.Filters;

namespace TechBlog.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IRecaptchaService _recaptchaService;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager, 
            ILogger<LoginModel> logger,
            UserManager<ApplicationUser> userManager,
            IRecaptchaService recaptchaService)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
            _recaptchaService = recaptchaService;
        }

        [BindProperty]
        public InputModel Input { get; set; }
        
        [BindProperty]
        public string RecaptchaResponse { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Check if reCAPTCHA should be enabled for login
            var recaptchaSettings = await _recaptchaService.GetSettingsAsync();
            bool recaptchaEnabled = recaptchaSettings?.IsEnabled == true && 
                                 recaptchaSettings.EnableForLogin &&
                                 !string.IsNullOrEmpty(recaptchaSettings.SiteKey) &&
                                 !string.IsNullOrEmpty(recaptchaSettings.SecretKey);

            if (recaptchaEnabled)
            {
                ViewData["RecaptchaSiteKey"] = recaptchaSettings.SiteKey;
            }

            ReturnUrl = returnUrl;
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            // Get reCAPTCHA settings
            var recaptchaSettings = await _recaptchaService.GetSettingsAsync();
            bool recaptchaEnabled = recaptchaSettings?.IsEnabled == true && 
                                 recaptchaSettings.EnableForLogin &&
                                 !string.IsNullOrEmpty(recaptchaSettings.SiteKey) &&
                                 !string.IsNullOrEmpty(recaptchaSettings.SecretKey);

            // If reCAPTCHA is enabled, validate it
            if (recaptchaEnabled)
            {
                var recaptchaResponse = Request.Form["g-recaptcha-response"];
                if (string.IsNullOrEmpty(recaptchaResponse))
                {
                    ModelState.AddModelError(string.Empty, "Please complete the reCAPTCHA validation.");
                    ViewData["RecaptchaSiteKey"] = recaptchaSettings.SiteKey;
                    return Page();
                }

                try
                {
                    // Pass the action parameter as required by the interface
                    // The service will handle the v2/v3 differences internally
                    var isValid = await _recaptchaService.VerifyCaptchaAsync(recaptchaResponse, "login");
                    if (!isValid)
                    {
                        _logger.LogWarning("reCAPTCHA validation failed for login attempt from {RemoteIpAddress}", 
                            HttpContext.Connection.RemoteIpAddress?.ToString());
                        ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed. Please try again.");
                        ViewData["RecaptchaSiteKey"] = recaptchaSettings.SiteKey;
                        return Page();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "reCAPTCHA validation error");
                    ModelState.AddModelError(string.Empty, "Error validating reCAPTCHA. Please try again.");
                    ViewData["RecaptchaSiteKey"] = recaptchaSettings.SiteKey;
                    return Page();
                }
            }

            // Proceed with login
            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                return LocalRedirect(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return RedirectToPage("./Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                
                // If we got this far, something failed, redisplay form with reCAPTCHA key if needed
                if (recaptchaEnabled)
                {
                    ViewData["RecaptchaSiteKey"] = recaptchaSettings.SiteKey;
                }
                
                return Page();
            }
        }
    }
}
