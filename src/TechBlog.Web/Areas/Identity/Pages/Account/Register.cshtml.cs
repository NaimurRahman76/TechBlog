using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces.Services;
using TechBlog.Web.Filters;

namespace TechBlog.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IRecaptchaService _recaptchaService;
        private readonly IEmailService _emailService;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IRecaptchaService recaptchaService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _recaptchaService = recaptchaService;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public class InputModel
        {
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
            [Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            
            // Add reCAPTCHA site key to ViewData
            var recaptchaSettings = await _recaptchaService.GetSettingsAsync();
            if (recaptchaSettings != null && recaptchaSettings.IsEnabled)
            {
                ViewData["RecaptchaSiteKey"] = recaptchaSettings.SiteKey;
            }
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            
            // Check reCAPTCHA if enabled
            var recaptchaSettings = await _recaptchaService.GetSettingsAsync();
            bool recaptchaEnabled = recaptchaSettings?.IsEnabled == true &&
                                 recaptchaSettings.EnableForRegistration &&
                                 !string.IsNullOrEmpty(recaptchaSettings.SiteKey) &&
                                 !string.IsNullOrEmpty(recaptchaSettings.SecretKey);

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
                    var isValid = await _recaptchaService.VerifyCaptchaAsync(recaptchaResponse, "register");
                    if (!isValid)
                    {
                        _logger.LogWarning("reCAPTCHA validation failed for registration attempt from {Email}", Input.Email);
                        ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed. Please try again.");
                        ViewData["RecaptchaSiteKey"] = recaptchaSettings.SiteKey;
                        return Page();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating reCAPTCHA for registration");
                    ModelState.AddModelError(string.Empty, "Error validating reCAPTCHA. Please try again.");
                    ViewData["RecaptchaSiteKey"] = recaptchaSettings.SiteKey;
                    return Page();
                }
            }
            // Continue with the registration process if reCAPTCHA validation passes
            
            if (ModelState.IsValid)
            {
                var user = CreateUser();
                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                
                // Check if email verification is enabled
                var emailSettings = await _emailService.GetSettingsAsync();
                bool requireEmailVerification = emailSettings?.IsEnabled == true && 
                                               emailSettings.EnableEmailVerification;
                
                user.EmailConfirmed = !requireEmailVerification; // Only auto-confirm if verification is disabled

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Add user to the "User" role by default
                    await _userManager.AddToRoleAsync(user, "User");

                    var userId = await _userManager.GetUserIdAsync(user);
                    
                    // Send email verification if enabled
                    if (requireEmailVerification)
                    {
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                            protocol: Request.Scheme);

                        var emailSent = await _emailService.SendEmailVerificationAsync(
                            Input.Email,
                            $"{user.FirstName} {user.LastName}",
                            callbackUrl);

                        if (emailSent)
                        {
                            _logger.LogInformation("Email verification sent to {Email}", Input.Email);
                            TempData["SuccessMessage"] = "Registration successful! Please check your email to verify your account.";
                            return RedirectToPage("./RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                        }
                        else
                        {
                            _logger.LogWarning("Failed to send verification email to {Email}", Input.Email);
                            TempData["WarningMessage"] = "Account created but failed to send verification email. Please contact support.";
                        }
                    }
                    else
                    {
                        // Auto sign-in the user after registration if email verification is disabled
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form with reCAPTCHA key
            recaptchaSettings = await _recaptchaService.GetSettingsAsync();
            if (recaptchaSettings != null && recaptchaSettings.IsEnabled)
            {
                ViewData["RecaptchaSiteKey"] = recaptchaSettings.SiteKey;
            }
            
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
