using System.Threading.Tasks;
using TechBlog.Core.Entities;

namespace TechBlog.Core.Interfaces.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email verification link to the user
        /// </summary>
        Task<bool> SendEmailVerificationAsync(string email, string userName, string verificationLink);
        
        /// <summary>
        /// Sends a generic email
        /// </summary>
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string plainTextBody = null);
        
        /// <summary>
        /// Gets the current email settings
        /// </summary>
        Task<EmailSettings> GetSettingsAsync();
        
        /// <summary>
        /// Updates email settings
        /// </summary>
        Task UpdateSettingsAsync(EmailSettings settings);
        
        /// <summary>
        /// Tests email configuration by sending a test email
        /// </summary>
        Task<bool> TestEmailConfigurationAsync(string testEmail);
    }
}
