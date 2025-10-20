using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TechBlog.Core.DTOs;
using TechBlog.Core.Entities;
using TechBlog.Core.Exceptions;
using TechBlog.Core.Interfaces;
using TechBlog.Core.Interfaces.Services;
using TechBlog.Web.Controllers;
using TechBlog.Web.Models;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TechBlog.Tests.Unit.Controllers
{
    public class BlogControllerTests
    {
        private readonly Mock<IBlogService> _mockBlogService;
        private readonly Mock<ICommentService> _mockCommentService;
        private readonly Mock<IWorkContext> _mockWorkContext;
        private readonly Mock<ILogger<BlogController>> _mockLogger;
        private readonly Mock<IRecaptchaService> _mockRecaptchaService;
        private readonly BlogController _controller;
        private readonly ApplicationUser _testUser;

        public BlogControllerTests()
        {
            _mockBlogService = new Mock<IBlogService>();
            _mockCommentService = new Mock<ICommentService>();
            _mockWorkContext = new Mock<IWorkContext>();
            _mockLogger = new Mock<ILogger<BlogController>>();
            _mockRecaptchaService = new Mock<IRecaptchaService>();
            
            _testUser = new ApplicationUser
            {
                Id = "test-user-id",
                UserName = "testuser@example.com",
                Email = "testuser@example.com"
            };

            // Setup default recaptcha verification to return true
            _mockRecaptchaService.Setup(x => x.VerifyCaptchaAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _controller = new BlogController(
                _mockBlogService.Object,
                Mock.Of<ICategoryService>(),
                Mock.Of<ITagService>(),
                _mockCommentService.Object,
                _mockWorkContext.Object,
                Mock.Of<IMapper>(),
                _mockLogger.Object,
                _mockRecaptchaService.Object);

            // Setup controller context for user identity
            var user = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, _testUser.UserName) },
                "TestAuthentication"));
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task AddComment_WithValidModel_ReturnsSuccess()
        {
            // Arrange
            var postId = 1;
            var comment = new Comment 
            { 
                Id = 1, 
                Content = "Test comment",
                AuthorName = _testUser.UserName,
                AuthorEmail = _testUser.Email,
                CreatedAt = DateTime.UtcNow,
                IsApproved = true
            };

            _mockWorkContext.Setup(w => w.IsAuthenticated).Returns(true);
            _mockWorkContext.Setup(w => w.IsAdmin).Returns(false);
            _mockWorkContext.Setup(w => w.IsAuthor).Returns(true);
            _mockWorkContext.Setup(w => w.GetCurrentUserAsync())
                .ReturnsAsync(_testUser);
                
            _mockBlogService.Setup(s => s.GetPostByIdAsync(postId))
                .ReturnsAsync(new BlogPost { Id = postId });
                
            _mockCommentService.Setup(s => s.CreateCommentAsync(It.IsAny<Comment>()))
                .ReturnsAsync(comment);

            var model = new AddCommentViewModel
            {
                PostId = postId,
                Content = comment.Content
            };

            // Add recaptcha response to form data
            var formCollection = new FormCollection(
                new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
                {
                    { "g-recaptcha-response", "test-recaptcha-response" }
                });

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _controller.ControllerContext.HttpContext.User,
                    Request = { Form = formCollection }
                }
            };

            // Act
            var result = await _controller.AddComment(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            Assert.True((bool)response.GetType().GetProperty("success").GetValue(response));
            
            _mockCommentService.Verify(s => s.CreateCommentAsync(It.Is<Comment>(c => 
                c.Content == model.Content &&
                c.AuthorId == _testUser.Id &&
                c.IsApproved == true)), 
                Times.Once);
        }

        [Fact]
        public async Task AddComment_WithInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Content", "Required");
            var model = new AddCommentViewModel();

            // Act
            var result = await _controller.AddComment(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value as dynamic;
            Assert.False((bool)response.GetType().GetProperty("success").GetValue(response));
        }

        [Fact]
        public async Task AddComment_ForNonExistentPost_ReturnsNotFound()
        {
            // Arrange
            var model = new AddCommentViewModel { PostId = 999, Content = "Test" };
            _mockBlogService.Setup(s => s.GetPostByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((BlogPost)null);

            // Act
            var result = await _controller.AddComment(model);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value as dynamic;
            Assert.False((bool)response.GetType().GetProperty("success").GetValue(response));
        }

        [Fact]
        public async Task AddComment_WithValidationException_ReturnsBadRequest()
        {
            // Arrange
            var model = new AddCommentViewModel { PostId = 1, Content = "Test" };
            _mockBlogService.Setup(s => s.GetPostByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new BlogPost { Id = 1 });
                
            _mockCommentService.Setup(s => s.CreateCommentAsync(It.IsAny<Comment>()))
                .ThrowsAsync(new ValidationException("Validation failed"));

            // Act
            var result = await _controller.AddComment(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value as dynamic;
            Assert.False((bool)response.GetType().GetProperty("success").GetValue(response));
        }

        [Fact]
        public async Task AddComment_WithException_ReturnsServerError()
        {
            // Arrange
            var model = new AddCommentViewModel { PostId = 1, Content = "Test" };
            _mockBlogService.Setup(s => s.GetPostByIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.AddComment(model);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }

        [Fact]
        public async Task AddComment_ForUnauthenticatedUser_UsesProvidedInfo()
        {
            // Arrange
            var model = new AddCommentViewModel 
            { 
                PostId = 1, 
                Content = "Test comment",
                AuthorName = "Test User",
                AuthorEmail = "test@example.com"
            };

            _mockWorkContext.Setup(w => w.IsAuthenticated).Returns(false);
            _mockBlogService.Setup(s => s.GetPostByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new BlogPost { Id = 1 });
                
            _mockCommentService.Setup(s => s.CreateCommentAsync(It.IsAny<Comment>()))
                .ReturnsAsync(new Comment { Id = 1 });

            // Act
            var result = await _controller.AddComment(model);

            // Assert
            _mockCommentService.Verify(s => s.CreateCommentAsync(It.Is<Comment>(c => 
                c.AuthorName == model.AuthorName &&
                c.AuthorEmail == model.AuthorEmail &&
                c.IsApproved == false)), // Should not be auto-approved for anonymous users
                Times.Once);
        }
    }
}
