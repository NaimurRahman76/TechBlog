using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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
        });

        builder.UseEnvironment("Development");
    }
}
