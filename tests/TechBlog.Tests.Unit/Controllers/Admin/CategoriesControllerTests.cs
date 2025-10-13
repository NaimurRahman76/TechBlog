using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechBlog.Core.DTOs;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;
using TechBlog.Web.Areas.Admin.Controllers;
using AutoMapper;
using Xunit;

namespace TechBlog.Tests.Unit.Controllers.Admin
{
    public class CategoriesControllerTests
    {
        private readonly Mock<ICategoryService> _categoryServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly CategoriesController _controller;

        public CategoriesControllerTests()
        {
            _categoryServiceMock = new Mock<ICategoryService>();
            _mapperMock = new Mock<IMapper>();

            _controller = new CategoriesController(_categoryServiceMock.Object, _mapperMock.Object);

            // Setup controller context with admin user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-user-id"),
                new Claim(ClaimTypes.Name, "admin@test.com"),
                new Claim(ClaimTypes.Email, "admin@test.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task Details_WithValidId_ShouldReturnViewWithCategory()
        {
            // Arrange
            var category = new Category
            {
                Id = 1,
                Name = "Technology",
                Description = "Tech related posts",
                Slug = "technology",
                CreatedAt = new DateTime(2023, 1, 1),
                UpdatedAt = new DateTime(2023, 1, 2),
                BlogPosts = new List<BlogPost>
                {
                    new BlogPost
                    {
                        Id = 1,
                        Title = "Test Post 1",
                        Slug = "test-post-1",
                        CreatedAt = new DateTime(2023, 1, 1),
                        IsPublished = true
                    },
                    new BlogPost
                    {
                        Id = 2,
                        Title = "Test Post 2",
                        Slug = "test-post-2",
                        CreatedAt = new DateTime(2023, 1, 2),
                        IsPublished = true
                    }
                }
            };

            _categoryServiceMock.Setup(s => s.GetCategoryByIdWithPostsAsync(1, false))
                               .ReturnsAsync(category);

            // Act
            var result = await _controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CategoryDetailDto>(viewResult.Model);
            
            Assert.Equal(1, model.Id);
            Assert.Equal("Technology", model.Name);
            Assert.Equal("Tech related posts", model.Description);
            Assert.Equal(2, model.PostsCount);
            Assert.Equal(2, model.RecentPosts.Count);
            Assert.Equal("Test Post 2", model.RecentPosts.First().Title); // Should be ordered by CreatedAt desc
            
            _categoryServiceMock.Verify(s => s.GetCategoryByIdWithPostsAsync(1, false), Times.Once);
        }

        [Fact]
        public async Task Details_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            _categoryServiceMock.Setup(s => s.GetCategoryByIdWithPostsAsync(999, false))
                               .ReturnsAsync((Category)null);

            // Act
            var result = await _controller.Details(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _categoryServiceMock.Verify(s => s.GetCategoryByIdWithPostsAsync(999, false), Times.Once);
        }

        [Fact]
        public async Task Details_WithZeroId_ShouldReturnNotFound()
        {
            // Act
            var result = await _controller.Details(0);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _categoryServiceMock.Verify(s => s.GetCategoryByIdWithPostsAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }
    }
}
