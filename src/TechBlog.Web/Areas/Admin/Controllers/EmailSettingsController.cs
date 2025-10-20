using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces.Services;

namespace TechBlog.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EmailSettingsController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailSettingsController> _logger;

        public EmailSettingsController(
            IEmailService emailService,
            ILogger<EmailSettingsController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var settings = await _emailService.GetSettingsAsync();
                return View(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading email settings");
                TempData["ErrorMessage"] = "An error occurred while loading email settings. Please try again later.";
                return View(new EmailSettings());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(EmailSettings model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                return View("Index", model);
            }

            try
            {
                await _emailService.UpdateSettingsAsync(model);
                TempData["SuccessMessage"] = "Email settings updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email settings");
                TempData["ErrorMessage"] = "An error occurred while updating email settings. Please try again.";
                return View("Index", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestEmail(string testEmail)
        {
            if (string.IsNullOrWhiteSpace(testEmail))
            {
                return Json(new { success = false, message = "Please provide a valid email address." });
            }

            try
            {
                var result = await _emailService.TestEmailConfigurationAsync(testEmail);
                
                if (result)
                {
                    _logger.LogInformation("Test email sent successfully to {Email}", testEmail);
                    return Json(new { success = true, message = $"Test email sent successfully to {testEmail}. Please check your inbox." });
                }
                else
                {
                    _logger.LogWarning("Failed to send test email to {Email}", testEmail);
                    return Json(new { success = false, message = "Failed to send test email. Please check your SMTP settings and try again." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email to {Email}", testEmail);
                return Json(new { success = false, message = $"Error sending test email: {ex.Message}" });
            }
        }
    }
}
