using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TechBlog.Core.DTOs;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;
using TechBlog.Core.Interfaces.Services;
using TechBlog.Web.Controllers;
using TechBlog.Web.Models;
using Xunit;

namespace TechBlog.Tests.Unit.Controllers
{
    public class BlogCategoriesTests
    {
        private readonly Mock<IBlogService> _mockBlogService;
        private readonly Mock<ICategoryService> _mockCategoryService;
        private readonly Mock<ITagService> _mockTagService;
        private readonly Mock<ICommentService> _mockCommentService;
        private readonly Mock<IWorkContext> _mockWorkContext;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<BlogController>> _mockLogger;
        private readonly Mock<IRecaptchaService> _mockRecaptchaService;
        private readonly BlogController _controller;

        public BlogCategoriesTests()
        {
            _mockBlogService = new Mock<IBlogService>();
            _mockCategoryService = new Mock<ICategoryService>();
            _mockTagService = new Mock<ITagService>();
            _mockCommentService = new Mock<ICommentService>();
            _mockWorkContext = new Mock<IWorkContext>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<BlogController>>();
            _mockRecaptchaService = new Mock<IRecaptchaService>();
            
            // Setup default recaptcha verification to return true
            _mockRecaptchaService.Setup(x => x.VerifyCaptchaAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _controller = new BlogController(
                _mockBlogService.Object,
                _mockCategoryService.Object,
                _mockTagService.Object,
                _mockCommentService.Object,
                _mockWorkContext.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockRecaptchaService.Object);
        }

        [Fact]
        public async Task Categories_ReturnsViewWithCategories()
        {
            // Arrange
            var testCategories = new List<Category>
            {
                new Category { Id = 1, Name = "Category 1", Slug = "category-1", BlogPosts = new List<BlogPost> { new BlogPost() } },
                new Category { Id = 2, Name = "Category 2", Slug = "category-2", BlogPosts = new List<BlogPost> { new BlogPost(), new BlogPost() } }
            };

            _mockCategoryService.Setup(s => s.GetAllCategoriesAsync())
                .ReturnsAsync(testCategories);

            // Act
            var result = await _controller.Categories();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<CategoryDto>>(viewResult.Model);
            Assert.Equal(2, model.Count());
            Assert.Equal(1, model.First().PostsCount);
            Assert.Equal(2, model.Last().PostsCount);
        }

        [Fact]
        public async Task Categories_WithException_ReturnsServerError()
        {
            // Arrange
            _mockCategoryService.Setup(s => s.GetAllCategoriesAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Categories();

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error retrieving categories")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }
    }
}
