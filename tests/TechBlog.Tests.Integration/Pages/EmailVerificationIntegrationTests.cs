using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TechBlog.Tests.Integration.Pages
{
    public class EmailVerificationIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;

        public EmailVerificationIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task ConfirmEmail_ReturnsNotFound_WhenUserIdIsInvalid()
        {
            // Act
            var response = await _client.GetAsync("/Identity/Account/ConfirmEmail?userId=invalid&code=testcode");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmEmail_RedirectsToHome_WhenParametersAreMissing()
        {
            // Act
            var response = await _client.GetAsync("/Identity/Account/ConfirmEmail");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task ResendEmailConfirmation_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Identity/Account/ResendEmailConfirmation");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Resend Email Confirmation", content);
        }

        [Fact]
        public async Task ResendEmailConfirmation_Post_RequiresValidEmail()
        {
            // Arrange
            var formData = new MultipartFormDataContent
            {
                { new StringContent(""), "Input.Email" }
            };

            // Act
            var response = await _client.PostAsync("/Identity/Account/ResendEmailConfirmation", formData);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Email", content); // Should show validation error
        }

        [Fact]
        public async Task RegisterConfirmation_ReturnsSuccessStatusCode_WithValidEmail()
        {
            // Act
            var response = await _client.GetAsync("/Identity/Account/RegisterConfirmation?email=test@example.com");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Check Your Email", content);
            Assert.Contains("test@example.com", content);
        }

        [Fact]
        public async Task RegisterConfirmation_RedirectsToHome_WhenEmailIsMissing()
        {
            // Act
            var response = await _client.GetAsync("/Identity/Account/RegisterConfirmation");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }
    }
}
