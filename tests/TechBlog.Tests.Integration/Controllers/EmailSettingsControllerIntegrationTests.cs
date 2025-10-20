using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TechBlog.Tests.Integration.Controllers
{
    public class EmailSettingsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;

        public EmailSettingsControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task Index_RequiresAuthentication()
        {
            // Act
            var response = await _client.GetAsync("/Admin/EmailSettings/Index");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Identity/Account/Login", response.Headers.Location?.ToString());
        }

        [Fact]
        public async Task Index_ReturnsSuccessForAuthenticatedAdmin()
        {
            // Arrange
            var authenticatedClient = await _factory.GetAuthenticatedClientAsync("Admin");

            // Act
            var response = await authenticatedClient.GetAsync("/Admin/EmailSettings/Index");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Email Settings", content);
            Assert.Contains("SMTP", content);
        }

        [Fact]
        public async Task Update_RequiresAuthentication()
        {
            // Arrange
            var formData = new MultipartFormDataContent
            {
                { new StringContent("smtp.test.com"), "SmtpHost" },
                { new StringContent("587"), "SmtpPort" },
                { new StringContent("test@test.com"), "FromEmail" },
                { new StringContent("Test"), "FromName" },
                { new StringContent("testuser"), "Username" },
                { new StringContent("testpass"), "Password" }
            };

            // Act
            var response = await _client.PostAsync("/Admin/EmailSettings/Update", formData);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Identity/Account/Login", response.Headers.Location?.ToString());
        }

        [Fact]
        public async Task TestEmail_RequiresAuthentication()
        {
            // Arrange
            var formData = new MultipartFormDataContent
            {
                { new StringContent("test@example.com"), "testEmail" }
            };

            // Act
            var response = await _client.PostAsync("/Admin/EmailSettings/TestEmail", formData);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }
    }
}
