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
using TechBlog.Web.Controllers;
using TechBlog.Web.Models;
using Xunit;

namespace TechBlog.Tests.Unit.Controllers
{
    public class HomeControllerTests
    {
        private readonly Mock<ILogger<HomeController>> _loggerMock;
        private readonly Mock<IBlogService> _blogServiceMock;
        private readonly Mock<ICategoryService> _categoryServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _loggerMock = new Mock<ILogger<HomeController>>();
            _blogServiceMock = new Mock<IBlogService>();
            _categoryServiceMock = new Mock<ICategoryService>();
            _mapperMock = new Mock<IMapper>();
            _controller = new HomeController(_loggerMock.Object, _blogServiceMock.Object, _categoryServiceMock.Object, _mapperMock.Object);
        }

        [Fact]
        public async Task Index_ReturnsViewWithCorrectModel()
        {
            // Arrange
            var mockPosts = new List<BlogPost>
            {
                new BlogPost { Id = 1, Title = "Test Post 1", IsPublished = true },
                new BlogPost { Id = 2, Title = "Test Post 2", IsPublished = true }
            };

            var mockCategories = new List<Category>
            {
                new Category { Id = 1, Name = "Category 1" },
                new Category { Id = 2, Name = "Category 2" }
            };

            var mockPostDtos = new List<PostListDto>
            {
                new PostListDto { Id = 1, Title = "Test Post 1" },
                new PostListDto { Id = 2, Title = "Test Post 2" }
            };

            var mockCategoryDtos = new List<CategoryDto>
            {
                new CategoryDto { Id = 1, Name = "Category 1" },
                new CategoryDto { Id = 2, Name = "Category 2" }
            };

            _blogServiceMock.Setup(x => x.GetRecentPostsAsync(It.IsAny<int>(), It.IsAny<bool>()))
                           .ReturnsAsync(mockPosts);

            _categoryServiceMock.Setup(x => x.GetAllCategoriesAsync())
                               .ReturnsAsync(mockCategories);

            _mapperMock.Setup(x => x.Map<IEnumerable<PostListDto>>(mockPosts))
                      .Returns(mockPostDtos);

            _mapperMock.Setup(x => x.Map<IEnumerable<CategoryDto>>(mockCategories))
                      .Returns(mockCategoryDtos);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<HomeViewModel>(viewResult.Model);
            Assert.Equal(2, model.RecentPosts.Count());
            Assert.Equal(2, model.Categories.Count());
        }

        [Fact]
        public async Task Index_WithNoPosts_ReturnsViewWithEmptyModel()
        {
            // Arrange
            var emptyPosts = new List<BlogPost>();
            var emptyCategories = new List<Category>();

            _blogServiceMock.Setup(x => x.GetRecentPostsAsync(It.IsAny<int>(), It.IsAny<bool>()))
                           .ReturnsAsync(emptyPosts);

            _categoryServiceMock.Setup(x => x.GetAllCategoriesAsync())
                               .ReturnsAsync(emptyCategories);

            _mapperMock.Setup(x => x.Map<IEnumerable<PostListDto>>(emptyPosts))
                      .Returns(new List<PostListDto>());

            _mapperMock.Setup(x => x.Map<IEnumerable<CategoryDto>>(emptyCategories))
                      .Returns(new List<CategoryDto>());

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<HomeViewModel>(viewResult.Model);
            Assert.Empty(model.RecentPosts);
            Assert.Empty(model.Categories);
        }
    }
}
