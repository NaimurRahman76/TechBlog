using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces.Services;
using TechBlog.Infrastructure.Data;

namespace TechBlog.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailService> _logger;

        public EmailService(ApplicationDbContext context, ILogger<EmailService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> SendEmailVerificationAsync(string email, string userName, string verificationLink)
        {
            var settings = await GetSettingsAsync();
            
            if (settings == null || !settings.IsEnabled || !settings.EnableEmailVerification)
            {
                _logger.LogWarning("Email verification is disabled or settings not configured");
                return false;
            }

            var subject = "Verify Your Email Address - TechBlog";
            var htmlBody = GetEmailVerificationTemplate(userName, verificationLink);
            var plainTextBody = $"Hello {userName},\n\nPlease verify your email address by clicking the following link:\n{verificationLink}\n\nThis link will expire in {settings.VerificationLinkExpirationHours} hours.\n\nIf you did not create an account, please ignore this email.\n\nBest regards,\nTechBlog Team";

            return await SendEmailAsync(email, subject, htmlBody, plainTextBody);
        }

        public async Task<bool> SendPasswordResetAsync(string email, string userName, string resetLink)
        {
            var settings = await GetSettingsAsync();
            
            if (settings == null || !settings.IsEnabled)
            {
                _logger.LogWarning("Email service is disabled or settings not configured");
                return false;
            }

            var subject = "Reset Your Password - TechBlog";
            var htmlBody = GetPasswordResetTemplate(userName, resetLink, settings.PasswordResetLinkExpirationHours);
            var plainTextBody = $"Hello {userName},\n\nWe received a request to reset your password. Click the following link to reset your password:\n{resetLink}\n\nThis link will expire in {settings.PasswordResetLinkExpirationHours} hour(s).\n\nIf you did not request a password reset, please ignore this email or contact support if you have concerns.\n\nBest regards,\nTechBlog Team";

            return await SendEmailAsync(email, subject, htmlBody, plainTextBody);
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string plainTextBody = null)
        {
            try
            {
                var settings = await GetSettingsAsync();
                
                if (settings == null || !settings.IsEnabled)
                {
                    _logger.LogWarning("Email service is disabled or not configured");
                    return false;
                }

                using var smtpClient = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
                {
                    EnableSsl = settings.EnableSsl,
                    Credentials = new NetworkCredential(settings.Username, settings.Password),
                    Timeout = 30000 // 30 seconds
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(settings.FromEmail, settings.FromName),
                    Subject = subject,
                    IsBodyHtml = true,
                    Body = htmlBody
                };

                mailMessage.To.Add(toEmail);

                // Add plain text alternative if provided
                if (!string.IsNullOrEmpty(plainTextBody))
                {
                    var plainView = AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain");
                    var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
                    mailMessage.AlternateViews.Add(plainView);
                    mailMessage.AlternateViews.Add(htmlView);
                }

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                return true;
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error sending email to {Email}: {Message}", toEmail, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<EmailSettings> GetSettingsAsync()
        {
            var settings = await _context.Set<EmailSettings>().FirstOrDefaultAsync();
            
            if (settings == null)
            {
                // Return default settings if none exist
                settings = new EmailSettings
                {
                    Id = 1,
                    SmtpHost = "smtp.gmail.com",
                    SmtpPort = 587,
                    FromEmail = "noreply@techblog.com",
                    FromName = "TechBlog",
                    Username = "",
                    Password = "",
                    EnableSsl = true,
                    EnableEmailVerification = true,
                    IsEnabled = false,
                    VerificationLinkExpirationHours = 24,
                    CreatedAt = DateTime.UtcNow
                };
            }
            
            return settings;
        }

        public async Task UpdateSettingsAsync(EmailSettings settings)
        {
            var existingSettings = await _context.Set<EmailSettings>().FirstOrDefaultAsync();
            
            if (existingSettings == null)
            {
                settings.CreatedAt = DateTime.UtcNow;
                _context.Set<EmailSettings>().Add(settings);
            }
            else
            {
                existingSettings.SmtpHost = settings.SmtpHost;
                existingSettings.SmtpPort = settings.SmtpPort;
                existingSettings.FromEmail = settings.FromEmail;
                existingSettings.FromName = settings.FromName;
                existingSettings.Username = settings.Username;
                
                // Only update password if a new one is provided
                if (!string.IsNullOrEmpty(settings.Password))
                {
                    existingSettings.Password = settings.Password;
                }
                
                existingSettings.EnableSsl = settings.EnableSsl;
                existingSettings.EnableEmailVerification = settings.EnableEmailVerification;
                existingSettings.IsEnabled = settings.IsEnabled;
                existingSettings.VerificationLinkExpirationHours = settings.VerificationLinkExpirationHours;
                existingSettings.PasswordResetLinkExpirationHours = settings.PasswordResetLinkExpirationHours;
                existingSettings.UpdatedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Email settings updated successfully");
        }

        public async Task<bool> TestEmailConfigurationAsync(string testEmail)
        {
            var subject = "TechBlog - Email Configuration Test";
            var htmlBody = @"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Email Configuration Test</h2>
                    <p>This is a test email to verify your email configuration is working correctly.</p>
                    <p>If you received this email, your SMTP settings are configured properly!</p>
                    <hr>
                    <p style='color: #666; font-size: 12px;'>TechBlog Email Service</p>
                </body>
                </html>";
            var plainTextBody = "This is a test email to verify your email configuration is working correctly.\n\nIf you received this email, your SMTP settings are configured properly!";

            return await SendEmailAsync(testEmail, subject, htmlBody, plainTextBody);
        }

        private string GetEmailVerificationTemplate(string userName, string verificationLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Verify Your Email</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f4f4f4; padding: 20px;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                    <!-- Header -->
                    <tr>
                        <td style='background-color: #007bff; padding: 30px; text-align: center;'>
                            <h1 style='color: #ffffff; margin: 0; font-size: 28px;'>TechBlog</h1>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px 30px;'>
                            <h2 style='color: #333333; margin-top: 0;'>Hello {userName}!</h2>
                            <p style='color: #666666; font-size: 16px; line-height: 1.6;'>
                                Thank you for registering with TechBlog. To complete your registration and activate your account, 
                                please verify your email address by clicking the button below.
                            </p>
                            
                            <table width='100%' cellpadding='0' cellspacing='0' style='margin: 30px 0;'>
                                <tr>
                                    <td align='center'>
                                        <a href='{verificationLink}' 
                                           style='display: inline-block; padding: 14px 40px; background-color: #007bff; 
                                                  color: #ffffff; text-decoration: none; border-radius: 4px; 
                                                  font-size: 16px; font-weight: bold;'>
                                            Verify Email Address
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            
                            <p style='color: #666666; font-size: 14px; line-height: 1.6;'>
                                If the button doesn't work, you can copy and paste the following link into your browser:
                            </p>
                            <p style='color: #007bff; font-size: 14px; word-break: break-all;'>
                                {verificationLink}
                            </p>
                            
                            <p style='color: #999999; font-size: 13px; margin-top: 30px;'>
                                <strong>Note:</strong> This verification link will expire in 24 hours.
                            </p>
                            
                            <p style='color: #666666; font-size: 14px; line-height: 1.6;'>
                                If you did not create an account with TechBlog, please ignore this email.
                            </p>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #f8f9fa; padding: 20px 30px; text-align: center; border-top: 1px solid #e9ecef;'>
                            <p style='color: #999999; font-size: 12px; margin: 0;'>
                                &copy; {DateTime.UtcNow.Year} TechBlog. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        private string GetPasswordResetTemplate(string userName, string resetLink, int expirationHours)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reset Your Password</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f4f4f4; padding: 20px;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                    <!-- Header -->
                    <tr>
                        <td style='background-color: #dc3545; padding: 30px; text-align: center;'>
                            <h1 style='color: #ffffff; margin: 0; font-size: 28px;'>TechBlog</h1>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px 30px;'>
                            <h2 style='color: #333333; margin-top: 0;'>Hello {userName}!</h2>
                            <p style='color: #666666; font-size: 16px; line-height: 1.6;'>
                                We received a request to reset your password for your TechBlog account. 
                                If you made this request, click the button below to reset your password.
                            </p>
                            
                            <table width='100%' cellpadding='0' cellspacing='0' style='margin: 30px 0;'>
                                <tr>
                                    <td align='center'>
                                        <a href='{resetLink}' 
                                           style='display: inline-block; padding: 14px 40px; background-color: #dc3545; 
                                                  color: #ffffff; text-decoration: none; border-radius: 4px; 
                                                  font-size: 16px; font-weight: bold;'>
                                            Reset Password
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            
                            <p style='color: #666666; font-size: 14px; line-height: 1.6;'>
                                If the button doesn't work, you can copy and paste the following link into your browser:
                            </p>
                            <p style='color: #dc3545; font-size: 14px; word-break: break-all;'>
                                {resetLink}
                            </p>
                            
                            <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 30px 0;'>
                                <p style='color: #856404; font-size: 14px; margin: 0; line-height: 1.6;'>
                                    <strong>⚠️ Important:</strong> This password reset link will expire in {expirationHours} hour(s) for security reasons.
                                </p>
                            </div>
                            
                            <div style='background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 15px; margin: 30px 0;'>
                                <p style='color: #721c24; font-size: 14px; margin: 0; line-height: 1.6;'>
                                    <strong>🔒 Security Notice:</strong> If you did not request a password reset, please ignore this email. 
                                    Your password will remain unchanged. If you're concerned about your account security, please contact our support team immediately.
                                </p>
                            </div>
                            
                            <p style='color: #666666; font-size: 14px; line-height: 1.6;'>
                                For security reasons, we never include passwords in emails. After clicking the reset link, 
                                you'll be able to create a new password for your account.
                            </p>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #f8f9fa; padding: 20px 30px; text-align: center; border-top: 1px solid #e9ecef;'>
                            <p style='color: #999999; font-size: 12px; margin: 0;'>
                                &copy; {DateTime.UtcNow.Year} TechBlog. All rights reserved.
                            </p>
                            <p style='color: #999999; font-size: 11px; margin: 10px 0 0 0;'>
                                This is an automated email. Please do not reply to this message.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}
