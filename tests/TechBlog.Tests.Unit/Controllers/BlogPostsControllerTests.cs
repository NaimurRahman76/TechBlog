using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TechBlog.Core.DTOs;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;
using TechBlog.Web.Areas.Admin.Controllers;
using TechBlog.Web.Areas.Admin.Models;
using AutoMapper;
using Xunit;

namespace TechBlog.Tests.Unit.Controllers
{
    public class BlogPostsControllerTests
    {
        private readonly Mock<IBlogService> _blogServiceMock;
        private readonly Mock<ICategoryService> _categoryServiceMock;
        private readonly Mock<ITagService> _tagServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IWebHostEnvironment> _webHostEnvironmentMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly BlogPostsController _controller;

        public BlogPostsControllerTests()
        {
            _blogServiceMock = new Mock<IBlogService>();
            _categoryServiceMock = new Mock<ICategoryService>();
            _tagServiceMock = new Mock<ITagService>();
            _userServiceMock = new Mock<IUserService>();
            _webHostEnvironmentMock = new Mock<IWebHostEnvironment>();
            _mapperMock = new Mock<IMapper>();

            // Mock UserManager
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

            _controller = new BlogPostsController(
                _blogServiceMock.Object,
                _categoryServiceMock.Object,
                _tagServiceMock.Object,
                _mapperMock.Object,
                _webHostEnvironmentMock.Object);

            // Setup controller context with admin user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-user-id"),
                new Claim(ClaimTypes.Name, "admin@test.com"),
                new Claim(ClaimTypes.Email, "admin@test.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Setup UserManager mock
            _userManagerMock.Setup(um => um.FindByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync((string email) => email == "admin@test.com"
                               ? new ApplicationUser { Id = "admin-user-id", Email = email, UserName = "admin" }
                               : null);
            _userManagerMock.Setup(um => um.FindByIdAsync(It.IsAny<string>()))
                           .ReturnsAsync((string id) => id == "admin-user-id"
                               ? new ApplicationUser { Id = id, Email = "admin@test.com", UserName = "admin" }
                               : null);
            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                           .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                           .ReturnsAsync(IdentityResult.Success);
        }

        [Fact]
        public async Task Index_WithPagination_ShouldReturnPagedViewModel()
        {
            // Arrange
            var posts = new List<BlogPost>
            {
                new BlogPost { Id = 1, Title = "Post 1", Content = "Content 1", IsPublished = true, CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new BlogPost { Id = 2, Title = "Post 2", Content = "Content 2", IsPublished = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new BlogPost { Id = 3, Title = "Post 3", Content = "Content 3", IsPublished = false, CreatedAt = DateTime.UtcNow }
            };

            var pagedList = new PagedList<BlogPost>(posts, 3, 1, 10);
            var postDtos = new List<PostAdminListDto>
            {
                new PostAdminListDto { Id = 1, Title = "Post 1", IsPublished = true },
                new PostAdminListDto { Id = 2, Title = "Post 2", IsPublished = true },
                new PostAdminListDto { Id = 3, Title = "Post 3", IsPublished = false }
            };

            _blogServiceMock.Setup(s => s.GetAllPostsAsync(true))
                           .ReturnsAsync(posts.AsQueryable());
            _mapperMock.Setup(m => m.Map<IEnumerable<PostAdminListDto>>(It.IsAny<IEnumerable<BlogPost>>()))
                      .Returns(postDtos);

            // Act
            var result = await _controller.Index(1, null, null, 10);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<BlogPostListViewModel>(viewResult.Model);
            Assert.Equal(3, model.TotalCount);
            Assert.Equal(1, model.CurrentPage);
            Assert.Equal(1, model.TotalPages);
            Assert.Equal(3, model.BlogPosts.Count());
        }

        [Fact]
        public async Task Index_WithSearchFilter_ShouldFilterPosts()
        {
            // Arrange
            var posts = new List<BlogPost>
            {
                new BlogPost { Id = 1, Title = "JavaScript Tutorial", Content = "Learn JS", IsPublished = true },
                new BlogPost { Id = 2, Title = "Python Guide", Content = "Python content", IsPublished = true },
                new BlogPost { Id = 3, Title = "CSS Tips", Content = "CSS content", IsPublished = false }
            };

            _blogServiceMock.Setup(s => s.GetAllPostsAsync(true))
                           .ReturnsAsync(posts.AsQueryable());

            // Act
            var result = await _controller.Index(1, "JavaScript", null, 10);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(viewResult.Model);
            _blogServiceMock.Verify(s => s.GetAllPostsAsync(true), Times.Once);
        }

        [Fact]
        public async Task Index_WithStatusFilter_ShouldFilterByPublishedStatus()
        {
            // Arrange
            var posts = new List<BlogPost>
            {
                new BlogPost { Id = 1, Title = "Published Post", Content = "Content", IsPublished = true },
                new BlogPost { Id = 2, Title = "Draft Post", Content = "Content", IsPublished = false }
            };

            _blogServiceMock.Setup(s => s.GetAllPostsAsync(true))
                           .ReturnsAsync(posts.AsQueryable());

            // Act
            var result = await _controller.Index(1, null, "published", 10);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            _blogServiceMock.Verify(s => s.GetAllPostsAsync(true), Times.Once);
        }

        [Fact]
        public async Task Create_Get_ShouldReturnViewWithDefaultModel()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Tech", Slug = "tech" }
            };

            _categoryServiceMock.Setup(s => s.GetAllCategoriesAsync())
                               .ReturnsAsync(categories);

            // Act
            var result = await _controller.Create();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CreatePostDto>(viewResult.Model);
            Assert.False(model.IsPublished); // Should default to false (draft)
        }

        [Fact]
        public async Task Create_Post_WithValidModel_ShouldCreatePostAndRedirectToEdit()
        {
            // Arrange
            var model = new CreatePostDto
            {
                Title = "Test Post",
                Content = "Test Content",
                Summary = "Test Summary",
                CategoryId = 1,
                IsPublished = false // Should be saved as draft
            };

            var createdPost = new BlogPost
            {
                Id = 1,
                Title = "Test Post",
                Content = "Test Content",
                IsPublished = false
            };

            _categoryServiceMock.Setup(s => s.GetCategoryByIdAsync(1))
                               .ReturnsAsync(new Category { Id = 1, Name = "Tech" });
            _blogServiceMock.Setup(s => s.CreatePostAsync(It.IsAny<BlogPost>(), It.IsAny<string[]>()))
                           .ReturnsAsync(createdPost);
            _mapperMock.Setup(m => m.Map<BlogPost>(It.IsAny<CreatePostDto>()))
                      .Returns(createdPost);

            // Act
            var result = await _controller.Create(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Edit", redirectResult.ActionName);
            Assert.Equal(1, redirectResult.RouteValues["id"]);
            _blogServiceMock.Verify(s => s.CreatePostAsync(It.Is<BlogPost>(p => !p.IsPublished), It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task Edit_Get_WithValidId_ShouldReturnViewWithModel()
        {
            // Arrange
            var post = new BlogPost
            {
                Id = 1,
                Title = "Test Post",
                Content = "Test Content",
                IsPublished = true,
                Slug = "test-post"
            };

            var updateDto = new UpdatePostDto
            {
                Id = 1,
                Title = "Test Post",
                Content = "Test Content",
                IsPublished = true,
                Slug = "test-post"
            };

            _blogServiceMock.Setup(s => s.GetPostByIdAsync(1))
                           .ReturnsAsync(post);
            _mapperMock.Setup(m => m.Map<UpdatePostDto>(post))
                      .Returns(updateDto);

            // Act
            var result = await _controller.Edit(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<UpdatePostDto>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.True(model.IsPublished);
        }

        [Fact]
        public async Task Edit_Post_WithValidModel_ShouldUpdatePost()
        {
            // Arrange
            var existingPost = new BlogPost
            {
                Id = 1,
                Title = "Original Title",
                IsPublished = false
            };

            var model = new UpdatePostDto
            {
                Id = 1,
                Title = "Updated Title",
                IsPublished = true
            };

            _blogServiceMock.Setup(s => s.GetPostByIdAsync(1))
                           .ReturnsAsync(existingPost);
            _blogServiceMock.Setup(s => s.UpdatePostAsync(It.IsAny<BlogPost>(), It.IsAny<string[]>()))
                           .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Edit(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Edit", redirectResult.ActionName);
            Assert.Equal(1, redirectResult.RouteValues["id"]);
            _blogServiceMock.Verify(s => s.UpdatePostAsync(It.Is<BlogPost>(p => p.IsPublished), It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task TogglePublish_ShouldTogglePublishedStatus()
        {
            // Arrange
            var post = new BlogPost
            {
                Id = 1,
                Title = "Test Post",
                IsPublished = false,
                CreatedAt = DateTime.UtcNow
            };

            _blogServiceMock.Setup(s => s.GetPostByIdAsync(1))
                           .ReturnsAsync(post);
            _blogServiceMock.Setup(s => s.UpdatePostAsync(It.IsAny<BlogPost>(), It.IsAny<string[]>()))
                           .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.TogglePublish(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Edit", redirectResult.ActionName);
            _blogServiceMock.Verify(s => s.UpdatePostAsync(It.Is<BlogPost>(p => p.IsPublished && p.PublishedAt.HasValue), It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task Delete_WithValidId_ShouldDeletePostAndRedirect()
        {
            // Arrange
            var post = new BlogPost { Id = 1, Title = "Test Post" };

            _blogServiceMock.Setup(s => s.GetPostByIdAsync(1))
                           .ReturnsAsync(post);
            _blogServiceMock.Setup(s => s.DeletePostAsync(1))
                           .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            _blogServiceMock.Verify(s => s.DeletePostAsync(1), Times.Once);
        }
    }
}
