using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using TechBlog.Infrastructure.Data;
using System.Net;

namespace TechBlog.Tests.Integration;

public class PageRenderingTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PageRenderingTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task HomePage_ShouldRenderSuccessfully()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("TechBlog");
        content.Should().Contain("Your go-to source for the latest in software development");
    }

    [Fact]
    public async Task AboutPage_ShouldRenderSuccessfully()
    {
        // Act
        var response = await _client.GetAsync("/Home/About");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("About TechBlog");
        content.Should().Contain("Our Mission");
    }

    [Fact]
    public async Task BlogPage_ShouldRenderSuccessfully()
    {
        // Act
        var response = await _client.GetAsync("/Blog");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Blog Posts");
        content.Should().Contain("No blog posts yet");
    }

    [Fact]
    public async Task LoginPage_ShouldRenderSuccessfully()
    {
        // Act
        var response = await _client.GetAsync("/Identity/Account/Login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Welcome Back");
        content.Should().Contain("Sign in to your TechBlog account");
    }

    [Fact]
    public async Task RegisterPage_ShouldRenderSuccessfully()
    {
        // Act
        var response = await _client.GetAsync("/Identity/Account/Register");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Create Account");
        content.Should().Contain("Join the TechBlog community");
    }

    [Fact]
    public async Task AdminDashboard_ShouldRequireAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/Admin/Dashboard");

        // Assert
        // Should redirect to login page
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("Identity/Account/Login");
    }

    [Fact]
    public async Task AdminBlogPosts_ShouldRequireAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/Admin/BlogPosts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("Identity/Account/Login");
    }

    [Fact]
    public async Task AdminCategories_ShouldRequireAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/Admin/Categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("Identity/Account/Login");
    }

    [Fact]
    public async Task AdminTags_ShouldRequireAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/Admin/Tags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("Identity/Account/Login");
    }

    [Fact]
    public async Task AdminComments_ShouldRequireAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/Admin/Comments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("Identity/Account/Login");
    }

    [Fact]
    public async Task NonExistentPage_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/NonExistent/Page");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/About")]
    [InlineData("/Blog")]
    [InlineData("/Identity/Account/Login")]
    [InlineData("/Identity/Account/Register")]
    public async Task PublicPages_ShouldNotContainErrors(string url)
    {
        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        
        // Check for common error indicators
        content.Should().NotContain("Exception");
        content.Should().NotContain("Error");
        content.Should().NotContain("500");
        content.Should().NotContain("The view");
        content.Should().NotContain("was not found");
    }

    [Fact]
    public async Task Layout_ShouldContainNavigationElements()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        
        // Check for navigation elements
        content.Should().Contain("navbar");
        content.Should().Contain("Home");
        content.Should().Contain("Blog");
        content.Should().Contain("About");
        content.Should().Contain("Login");
        content.Should().Contain("Register");
    }
}
