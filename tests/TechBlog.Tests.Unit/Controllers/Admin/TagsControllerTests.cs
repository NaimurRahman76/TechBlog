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
    public class TagsControllerTests
    {
        private readonly Mock<ITagService> _tagServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly TagsController _controller;

        public TagsControllerTests()
        {
            _tagServiceMock = new Mock<ITagService>();
            _mapperMock = new Mock<IMapper>();

            _controller = new TagsController(_tagServiceMock.Object, _mapperMock.Object);

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
        public async Task Details_WithValidId_ShouldReturnViewWithTag()
        {
            // Arrange
            var tag = new Tag
            {
                Id = 1,
                Name = "JavaScript",
                Slug = "javascript"
            };

            var tagDto = new TagDto
            {
                Id = 1,
                Name = "JavaScript",
                Slug = "javascript"
            };

            _tagServiceMock.Setup(s => s.GetTagByIdAsync(1))
                          .ReturnsAsync(tag);
            _mapperMock.Setup(m => m.Map<TagDto>(tag))
                      .Returns(tagDto);

            // Act
            var result = await _controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<TagDto>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("JavaScript", model.Name);
            _tagServiceMock.Verify(s => s.GetTagByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task Details_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            _tagServiceMock.Setup(s => s.GetTagByIdAsync(999))
                          .ReturnsAsync((Tag)null);

            // Act
            var result = await _controller.Details(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _tagServiceMock.Verify(s => s.GetTagByIdAsync(999), Times.Once);
        }

        [Fact]
        public async Task Details_WithZeroId_ShouldReturnNotFound()
        {
            // Arrange
            _tagServiceMock.Setup(s => s.GetTagByIdAsync(0))
                          .ReturnsAsync((Tag)null);

            // Act
            var result = await _controller.Details(0);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _tagServiceMock.Verify(s => s.GetTagByIdAsync(0), Times.Once);
        }
    }
}
