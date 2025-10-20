using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces.Services;
using TechBlog.Infrastructure.Data;

namespace TechBlog.Tests.Integration;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the app's ApplicationDbContext registration.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add ApplicationDbContext using an in-memory database for testing.
            var dbName = $"InMemoryDbForTesting_{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            // Mock the IRecaptchaService
            var mockRecaptchaService = new Mock<IRecaptchaService>();
            mockRecaptchaService
                .Setup(x => x.VerifyCaptchaAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Remove the existing IRecaptchaService registration if it exists
            var recaptchaDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IRecaptchaService));
            
            if (recaptchaDescriptor != null)
            {
                services.Remove(recaptchaDescriptor);
            }

            // Register the mock IRecaptchaService
            services.AddSingleton(mockRecaptchaService.Object);
            
            // Mock the IEmailService to prevent actual email sending during tests
            var mockEmailService = new Mock<IEmailService>();
            mockEmailService
                .Setup(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            mockEmailService
                .Setup(x => x.GetSettingsAsync())
                .ReturnsAsync(new EmailSettings
                {
                    Id = 1,
                    SmtpHost = "smtp.test.com",
                    SmtpPort = 587,
                    FromEmail = "test@test.com",
                    FromName = "Test",
                    IsEnabled = false, // Disabled for testing
                    EnableEmailVerification = false
                });

            // Remove the existing IEmailService registration if it exists
            var emailDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmailService));
            
            if (emailDescriptor != null)
            {
                services.Remove(emailDescriptor);
            }

            // Register the mock IEmailService
            services.AddSingleton(mockEmailService.Object);
        });

        builder.UseEnvironment("Development");
    }

    public async Task<HttpClient> GetAuthenticatedClientAsync(string role = "User")
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure role exists
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Create test user
        var testUser = new ApplicationUser
        {
            UserName = $"test{role}@test.com",
            Email = $"test{role}@test.com",
            FirstName = "Test",
            LastName = role,
            EmailConfirmed = true
        };

        var existingUser = await userManager.FindByEmailAsync(testUser.Email);
        if (existingUser == null)
        {
            await userManager.CreateAsync(testUser, "Test@123");
            await userManager.AddToRoleAsync(testUser, role);
        }

        return client;
    }
}
