using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TechBlog.Core.Entities;
using TechBlog.Infrastructure.Data;
using TechBlog.Infrastructure.Services;
using Xunit;

namespace TechBlog.Tests.Unit.Services
{
    public class EmailServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly EmailService _emailService;

        public EmailServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockLogger = new Mock<ILogger<EmailService>>();
            _emailService = new EmailService(_context, _mockLogger.Object);
        }

        [Fact]
        public async Task GetSettingsAsync_ReturnsDefaultSettings_WhenNoSettingsExist()
        {
            // Act
            var settings = await _emailService.GetSettingsAsync();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal("smtp.gmail.com", settings.SmtpHost);
            Assert.Equal(587, settings.SmtpPort);
            Assert.False(settings.IsEnabled);
            Assert.True(settings.EnableEmailVerification);
        }

        [Fact]
        public async Task GetSettingsAsync_ReturnsExistingSettings_WhenSettingsExist()
        {
            // Arrange
            var existingSettings = new EmailSettings
            {
                Id = 1,
                SmtpHost = "smtp.test.com",
                SmtpPort = 465,
                FromEmail = "test@test.com",
                FromName = "Test",
                Username = "testuser",
                Password = "testpass",
                EnableSsl = true,
                EnableEmailVerification = true,
                IsEnabled = true,
                VerificationLinkExpirationHours = 48,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailSettings.Add(existingSettings);
            await _context.SaveChangesAsync();

            // Act
            var settings = await _emailService.GetSettingsAsync();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal("smtp.test.com", settings.SmtpHost);
            Assert.Equal(465, settings.SmtpPort);
            Assert.Equal("test@test.com", settings.FromEmail);
            Assert.True(settings.IsEnabled);
            Assert.Equal(48, settings.VerificationLinkExpirationHours);
        }

        [Fact]
        public async Task UpdateSettingsAsync_CreatesNewSettings_WhenNoSettingsExist()
        {
            // Arrange
            var newSettings = new EmailSettings
            {
                Id = 1,
                SmtpHost = "smtp.newtest.com",
                SmtpPort = 587,
                FromEmail = "new@test.com",
                FromName = "NewTest",
                Username = "newuser",
                Password = "newpass",
                EnableSsl = true,
                EnableEmailVerification = true,
                IsEnabled = true,
                VerificationLinkExpirationHours = 24
            };

            // Act
            await _emailService.UpdateSettingsAsync(newSettings);

            // Assert
            var savedSettings = await _context.EmailSettings.FirstOrDefaultAsync();
            Assert.NotNull(savedSettings);
            Assert.Equal("smtp.newtest.com", savedSettings.SmtpHost);
            Assert.Equal("new@test.com", savedSettings.FromEmail);
        }

        [Fact]
        public async Task UpdateSettingsAsync_UpdatesExistingSettings_WhenSettingsExist()
        {
            // Arrange
            var existingSettings = new EmailSettings
            {
                Id = 1,
                SmtpHost = "smtp.old.com",
                SmtpPort = 587,
                FromEmail = "old@test.com",
                FromName = "Old",
                Username = "olduser",
                Password = "oldpass",
                EnableSsl = true,
                EnableEmailVerification = false,
                IsEnabled = false,
                VerificationLinkExpirationHours = 24,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailSettings.Add(existingSettings);
            await _context.SaveChangesAsync();

            var updatedSettings = new EmailSettings
            {
                Id = 1,
                SmtpHost = "smtp.updated.com",
                SmtpPort = 465,
                FromEmail = "updated@test.com",
                FromName = "Updated",
                Username = "updateduser",
                Password = "updatedpass",
                EnableSsl = false,
                EnableEmailVerification = true,
                IsEnabled = true,
                VerificationLinkExpirationHours = 48
            };

            // Act
            await _emailService.UpdateSettingsAsync(updatedSettings);

            // Assert
            var savedSettings = await _context.EmailSettings.FirstOrDefaultAsync();
            Assert.NotNull(savedSettings);
            Assert.Equal("smtp.updated.com", savedSettings.SmtpHost);
            Assert.Equal(465, savedSettings.SmtpPort);
            Assert.Equal("updated@test.com", savedSettings.FromEmail);
            Assert.True(savedSettings.IsEnabled);
            Assert.True(savedSettings.EnableEmailVerification);
            Assert.Equal(48, savedSettings.VerificationLinkExpirationHours);
            Assert.NotNull(savedSettings.UpdatedAt);
        }

        [Fact]
        public async Task UpdateSettingsAsync_DoesNotUpdatePassword_WhenPasswordIsEmpty()
        {
            // Arrange
            var existingSettings = new EmailSettings
            {
                Id = 1,
                SmtpHost = "smtp.test.com",
                SmtpPort = 587,
                FromEmail = "test@test.com",
                FromName = "Test",
                Username = "testuser",
                Password = "originalpassword",
                EnableSsl = true,
                EnableEmailVerification = true,
                IsEnabled = true,
                VerificationLinkExpirationHours = 24,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailSettings.Add(existingSettings);
            await _context.SaveChangesAsync();

            var updatedSettings = new EmailSettings
            {
                Id = 1,
                SmtpHost = "smtp.updated.com",
                SmtpPort = 587,
                FromEmail = "updated@test.com",
                FromName = "Updated",
                Username = "updateduser",
                Password = "", // Empty password
                EnableSsl = true,
                EnableEmailVerification = true,
                IsEnabled = true,
                VerificationLinkExpirationHours = 24
            };

            // Act
            await _emailService.UpdateSettingsAsync(updatedSettings);

            // Assert
            var savedSettings = await _context.EmailSettings.FirstOrDefaultAsync();
            Assert.NotNull(savedSettings);
            Assert.Equal("originalpassword", savedSettings.Password); // Password should remain unchanged
        }

        [Fact]
        public async Task SendEmailVerificationAsync_ReturnsFalse_WhenEmailServiceIsDisabled()
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
                EnableEmailVerification = true,
                IsEnabled = false, // Service disabled
                VerificationLinkExpirationHours = 24,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailSettings.Add(settings);
            await _context.SaveChangesAsync();

            // Act
            var result = await _emailService.SendEmailVerificationAsync(
                "user@test.com",
                "Test User",
                "http://test.com/verify");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SendEmailVerificationAsync_ReturnsFalse_WhenEmailVerificationIsDisabled()
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
                EnableEmailVerification = false, // Verification disabled
                IsEnabled = true,
                VerificationLinkExpirationHours = 24,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailSettings.Add(settings);
            await _context.SaveChangesAsync();

            // Act
            var result = await _emailService.SendEmailVerificationAsync(
                "user@test.com",
                "Test User",
                "http://test.com/verify");

            // Assert
            Assert.False(result);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
