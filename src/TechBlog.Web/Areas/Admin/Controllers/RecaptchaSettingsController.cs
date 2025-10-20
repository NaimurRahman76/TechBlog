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
    public class RecaptchaSettingsController : Controller
    {
        private readonly IRecaptchaService _recaptchaService;
        private readonly ILogger<RecaptchaSettingsController> _logger;

        public RecaptchaSettingsController(
            IRecaptchaService recaptchaService,
            ILogger<RecaptchaSettingsController> logger)
        {
            _recaptchaService = recaptchaService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var settings = await _recaptchaService.GetSettingsAsync();
                return View(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reCAPTCHA settings");
                TempData["ErrorMessage"] = "An error occurred while loading reCAPTCHA settings. Please try again later.";
                return View(new RecaptchaSettings());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(RecaptchaSettings model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                return View("Index", model);
            }

            try
            {
                await _recaptchaService.UpdateSettingsAsync(model);
                TempData["SuccessMessage"] = "reCAPTCHA settings updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reCAPTCHA settings");
                TempData["ErrorMessage"] = "An error occurred while updating reCAPTCHA settings. Please try again.";
                return View("Index", model);
            }
        }
    }
}
