using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces.Services;
using TechBlog.Web.Areas.Admin.Controllers;
using Xunit;

namespace TechBlog.Tests.Unit.Controllers
{
    public class EmailSettingsControllerTests
    {
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<EmailSettingsController>> _mockLogger;
        private readonly EmailSettingsController _controller;

        public EmailSettingsControllerTests()
        {
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<EmailSettingsController>>();
            _controller = new EmailSettingsController(_mockEmailService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithEmailSettings()
        {
            // Arrange
            var expectedSettings = new EmailSettings
            {
                Id = 1,
                SmtpHost = "smtp.test.com",
                SmtpPort = 587,
                FromEmail = "test@test.com",
                FromName = "Test",
                IsEnabled = true
            };

            _mockEmailService.Setup(s => s.GetSettingsAsync())
                .ReturnsAsync(expectedSettings);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<EmailSettings>(viewResult.Model);
            Assert.Equal(expectedSettings.SmtpHost, model.SmtpHost);
            Assert.Equal(expectedSettings.FromEmail, model.FromEmail);
        }

        [Fact]
        public async Task Index_ReturnsViewWithDefaultSettings_WhenExceptionOccurs()
        {
            // Arrange
            _mockEmailService.Setup(s => s.GetSettingsAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<EmailSettings>(viewResult.Model);
            Assert.NotNull(_controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task Update_RedirectsToIndex_WhenModelIsValid()
        {
            // Arrange
            var settings = new EmailSettings
            {
                Id = 1,
                SmtpHost = "smtp.test.com",
                SmtpPort = 587,
                FromEmail = "test@test.com",
                FromName = "Test",
                Username = "testuser",
                Password = "testpass",
                EnableSsl = true,
                IsEnabled = true
            };

            _mockEmailService.Setup(s => s.UpdateSettingsAsync(It.IsAny<EmailSettings>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(settings);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.NotNull(_controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task Update_ReturnsViewWithModel_WhenModelStateIsInvalid()
        {
            // Arrange
            var settings = new EmailSettings();
            _controller.ModelState.AddModelError("SmtpHost", "Required");

            // Act
            var result = await _controller.Update(settings);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.NotNull(_controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task Update_ReturnsViewWithError_WhenExceptionOccurs()
        {
            // Arrange
            var settings = new EmailSettings
            {
                Id = 1,
                SmtpHost = "smtp.test.com",
                SmtpPort = 587,
                FromEmail = "test@test.com",
                FromName = "Test",
                Username = "testuser",
                Password = "testpass"
            };

            _mockEmailService.Setup(s => s.UpdateSettingsAsync(It.IsAny<EmailSettings>()))
                .ThrowsAsync(new Exception("Update failed"));

            // Act
            var result = await _controller.Update(settings);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.NotNull(_controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task TestEmail_ReturnsJsonWithSuccess_WhenEmailSentSuccessfully()
        {
            // Arrange
            var testEmail = "test@example.com";
            _mockEmailService.Setup(s => s.TestEmailConfigurationAsync(testEmail))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.TestEmail(testEmail);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic value = jsonResult.Value;
            Assert.True(value.success);
            Assert.Contains("successfully", value.message.ToString());
        }

        [Fact]
        public async Task TestEmail_ReturnsJsonWithFailure_WhenEmailNotSent()
        {
            // Arrange
            var testEmail = "test@example.com";
            _mockEmailService.Setup(s => s.TestEmailConfigurationAsync(testEmail))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.TestEmail(testEmail);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic value = jsonResult.Value;
            Assert.False(value.success);
        }

        [Fact]
        public async Task TestEmail_ReturnsJsonWithError_WhenEmailIsEmpty()
        {
            // Act
            var result = await _controller.TestEmail("");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic value = jsonResult.Value;
            Assert.False(value.success);
            Assert.Contains("valid email", value.message.ToString());
        }

        [Fact]
        public async Task TestEmail_ReturnsJsonWithError_WhenExceptionOccurs()
        {
            // Arrange
            var testEmail = "test@example.com";
            _mockEmailService.Setup(s => s.TestEmailConfigurationAsync(testEmail))
                .ThrowsAsync(new Exception("SMTP error"));

            // Act
            var result = await _controller.TestEmail(testEmail);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic value = jsonResult.Value;
            Assert.False(value.success);
            Assert.Contains("Error", value.message.ToString());
        }
    }
}
