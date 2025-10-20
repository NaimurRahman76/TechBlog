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
    public class PasswordResetEmailServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly EmailService _emailService;

        public PasswordResetEmailServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockLogger = new Mock<ILogger<EmailService>>();
            _emailService = new EmailService(_context, _mockLogger.Object);
        }

        [Fact]
        public async Task SendPasswordResetAsync_ReturnsFalse_WhenEmailServiceIsDisabled()
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
                IsEnabled = false, // Service disabled
                PasswordResetLinkExpirationHours = 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailSettings.Add(settings);
            await _context.SaveChangesAsync();

            // Act
            var result = await _emailService.SendPasswordResetAsync(
                "user@test.com",
                "Test User",
                "http://test.com/reset");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SendPasswordResetAsync_ReturnsFalse_WhenSettingsAreNull()
        {
            // Act
            var result = await _emailService.SendPasswordResetAsync(
                "user@test.com",
                "Test User",
                "http://test.com/reset");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateSettingsAsync_UpdatesPasswordResetExpiration()
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
                Password = "testpass",
                EnableSsl = true,
                IsEnabled = true,
                VerificationLinkExpirationHours = 24,
                PasswordResetLinkExpirationHours = 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailSettings.Add(existingSettings);
            await _context.SaveChangesAsync();

            var updatedSettings = new EmailSettings
            {
                Id = 1,
                SmtpHost = "smtp.test.com",
                SmtpPort = 587,
                FromEmail = "test@test.com",
                FromName = "Test",
                Username = "testuser",
                Password = "testpass",
                EnableSsl = true,
                IsEnabled = true,
                VerificationLinkExpirationHours = 24,
                PasswordResetLinkExpirationHours = 2, // Changed from 1 to 2
            };

            // Act
            await _emailService.UpdateSettingsAsync(updatedSettings);

            // Assert
            var savedSettings = await _context.EmailSettings.FirstOrDefaultAsync();
            Assert.NotNull(savedSettings);
            Assert.Equal(2, savedSettings.PasswordResetLinkExpirationHours);
        }

        [Fact]
        public async Task GetSettingsAsync_ReturnsDefaultPasswordResetExpiration()
        {
            // Act
            var settings = await _emailService.GetSettingsAsync();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal(1, settings.PasswordResetLinkExpirationHours); // Default is 1 hour
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
