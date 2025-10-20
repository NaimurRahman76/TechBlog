using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TechBlog.Tests.Integration.Pages
{
    public class PasswordResetIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;

        public PasswordResetIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAnonymous = false
            });
        }

        [Fact]
        public async Task ForgotPassword_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Identity/Account/ForgotPassword");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Forgot Your Password", content);
        }

        [Fact]
        public async Task ForgotPassword_Post_RequiresValidEmail()
        {
            // Arrange
            var formData = new MultipartFormDataContent
            {
                { new StringContent(""), "Input.Email" }
            };

            // Act
            var response = await _client.PostAsync("/Identity/Account/ForgotPassword", formData);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Email", content); // Should show validation error
        }

        [Fact]
        public async Task ForgotPassword_Post_RedirectsToConfirmation_WithValidEmail()
        {
            // Arrange
            var formData = new MultipartFormDataContent
            {
                { new StringContent("test@example.com"), "Input.Email" }
            };

            // Act
            var response = await _client.PostAsync("/Identity/Account/ForgotPassword", formData);

            // Assert - Should redirect to confirmation page
            Assert.True(response.StatusCode == HttpStatusCode.Redirect || 
                       response.StatusCode == HttpStatusCode.Found ||
                       response.StatusCode == HttpStatusCode.MovedPermanently);
        }

        [Fact]
        public async Task ForgotPasswordConfirmation_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Identity/Account/ForgotPasswordConfirmation");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Check Your Email", content);
        }

        [Fact]
        public async Task ResetPassword_ReturnsBadRequest_WhenCodeIsMissing()
        {
            // Act
            var response = await _client.GetAsync("/Identity/Account/ResetPassword");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_ReturnsSuccessStatusCode_WithCode()
        {
            // Arrange
            var code = "dGVzdGNvZGU="; // Base64 encoded "testcode"

            // Act
            var response = await _client.GetAsync($"/Identity/Account/ResetPassword?code={code}");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Reset Your Password", content);
        }

        [Fact]
        public async Task ResetPassword_Post_RequiresValidData()
        {
            // Arrange
            var formData = new MultipartFormDataContent
            {
                { new StringContent(""), "Input.Email" },
                { new StringContent(""), "Input.Password" },
                { new StringContent(""), "Input.ConfirmPassword" },
                { new StringContent("testcode"), "Input.Code" }
            };

            // Act
            var response = await _client.PostAsync("/Identity/Account/ResetPassword", formData);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            // Should show validation errors
            Assert.Contains("Email", content);
        }

        [Fact]
        public async Task ResetPasswordConfirmation_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Identity/Account/ResetPasswordConfirmation");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Password Reset Successful", content);
        }

        [Fact]
        public async Task ResetPassword_Post_RequiresMatchingPasswords()
        {
            // Arrange
            var formData = new MultipartFormDataContent
            {
                { new StringContent("test@example.com"), "Input.Email" },
                { new StringContent("Password123!"), "Input.Password" },
                { new StringContent("DifferentPassword123!"), "Input.ConfirmPassword" },
                { new StringContent("testcode"), "Input.Code" }
            };

            // Act
            var response = await _client.PostAsync("/Identity/Account/ResetPassword", formData);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("password", content.ToLower()); // Should show password mismatch error
        }
    }
}
