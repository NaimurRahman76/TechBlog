using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces.Services;
using TechBlog.Infrastructure.Data;

namespace TechBlog.Infrastructure.Services
{
    public class RecaptchaService : IRecaptchaService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private const string RecaptchaVerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

        public RecaptchaService(
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _configuration = configuration;
        }

        public async Task<bool> VerifyCaptchaAsync(string token, string action = null)
        {
            try
            {
                var settings = await GetSettingsAsync();
                
                // If reCAPTCHA is disabled, skip verification
                if (!settings.IsEnabled)
                    return true;
                
                // Check if reCAPTCHA is enabled for the specific action
                if (!string.IsNullOrEmpty(action))
                {
                    bool isActionEnabled = action.ToLower() switch
                    {
                        "login" => settings.EnableForLogin,
                        "register" => settings.EnableForRegistration,
                        "comment" => settings.EnableForComments,
                        _ => true
                    };
                    
                    if (!isActionEnabled)
                        return true;
                }

                var secretKey = settings.SecretKey ?? _configuration["Recaptcha:SecretKey"];
                if (string.IsNullOrEmpty(secretKey))
                {
                    // Log error: Missing secret key
                    Console.WriteLine("reCAPTCHA secret key is not configured");
                    return false;
                }

                using var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.PostAsync($"{RecaptchaVerifyUrl}?secret={secretKey}&response={token}", null);
                
                if (!response.IsSuccessStatusCode)
                {
                    // Log error: API request failed
                    Console.WriteLine($"reCAPTCHA verification failed with status code: {response.StatusCode}");
                    return false;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RecaptchaResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    Console.WriteLine("Failed to deserialize reCAPTCHA response");
                    return false;
                }

                // For v2, we only need to check Success
                // For v3, we also check the score and action
                var isV3 = !string.IsNullOrEmpty(result.Action);
                
                var isValid = result.Success;
                
                if (isV3 && !string.IsNullOrEmpty(action))
                {
                    isValid = isValid && 
                             result.Score >= settings.ScoreThreshold && 
                             string.Equals(result.Action, action, StringComparison.OrdinalIgnoreCase);
                }
                
                if (!isValid)
                {
                    Console.WriteLine($"reCAPTCHA validation failed. Success: {result.Success}, " +
                                    $"Score: {result.Score}, Action: {result.Action}, " +
                                    $"ErrorCodes: {(result.ErrorCodes != null ? string.Join(", ", result.ErrorCodes) : "none")}");
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                Console.WriteLine($"Error in VerifyCaptchaAsync: {ex}");
                return false;
            }
        }

        public async Task<RecaptchaSettings> GetSettingsAsync()
        {
            return await _context.RecaptchaSettings.FirstOrDefaultAsync() ?? new RecaptchaSettings();
        }

        public async Task UpdateSettingsAsync(RecaptchaSettings settings)
        {
            try
            {
                var existingSettings = await _context.RecaptchaSettings.FirstOrDefaultAsync();
                
                if (existingSettings == null)
                {
                    settings.CreatedAt = DateTime.UtcNow;
                    await _context.RecaptchaSettings.AddAsync(settings);
                }
                else
                {
                    // Update all properties from the model
                    existingSettings.SiteKey = settings.SiteKey;
                    existingSettings.SecretKey = settings.SecretKey;
                    existingSettings.IsEnabled = settings.IsEnabled;
                    existingSettings.EnableForLogin = settings.EnableForLogin;
                    existingSettings.EnableForRegistration = settings.EnableForRegistration;
                    existingSettings.EnableForComments = settings.EnableForComments;
                    existingSettings.ScoreThreshold = settings.ScoreThreshold;
                    existingSettings.UpdatedAt = DateTime.UtcNow;
                    
                    _context.RecaptchaSettings.Update(existingSettings);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log the error (you might want to inject ILogger<RecaptchaService> for proper logging)
                Console.WriteLine($"Error updating reCAPTCHA settings: {ex.Message}");
                throw; // Re-throw to be handled by the controller
            }
        }
    }

    internal class RecaptchaResponse
    {
        public bool Success { get; set; }
        public float Score { get; set; }
        public string? Action { get; set; }
        public DateTime ChallengeTime { get; set; }
        public string? Hostname { get; set; }
        public string[]? ErrorCodes { get; set; }
    }
}
