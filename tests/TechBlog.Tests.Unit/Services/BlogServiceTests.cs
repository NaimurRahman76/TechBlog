using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;
using TechBlog.Infrastructure.Data;
using TechBlog.Infrastructure.Services;
using Xunit;

namespace TechBlog.Tests.Unit.Services
{
    public class BlogServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<BlogService>> _loggerMock;
        private readonly Mock<ITagService> _tagServiceMock;
        private readonly BlogService _blogService;

        public BlogServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<BlogService>>();
            _tagServiceMock = new Mock<ITagService>();
            _blogService = new BlogService(_context, _loggerMock.Object, _tagServiceMock.Object);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var category = new Category
            {
                Id = 1,
                Name = "Test Category",
                Description = "Test category description",
                Slug = "test-category",
                CreatedAt = DateTime.UtcNow
            };

            var author = new ApplicationUser
            {
                Id = "test-user-id",
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com"
            };

            var publishedPost = new BlogPost
            {
                Id = 1,
                Title = "Published Post",
                Slug = "published-post",
                Content = "This is a published post content",
                Summary = "Published post summary",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(-1),
                CategoryId = 1,
                Category = category,
                AuthorId = "test-user-id",
                Author = author,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ViewCount = 10
            };

            var draftPost = new BlogPost
            {
                Id = 2,
                Title = "Draft Post",
                Slug = "draft-post",
                Content = "This is a draft post content",
                Summary = "Draft post summary",
                IsPublished = false,
                CategoryId = 1,
                Category = category,
                AuthorId = "test-user-id",
                Author = author,
                CreatedAt = DateTime.UtcNow,
                ViewCount = 0
            };

            _context.Categories.Add(category);
            _context.Users.Add(author);
            _context.BlogPosts.AddRange(publishedPost, draftPost);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAllPostsAsync_WithoutIncludeUnpublished_ReturnsOnlyPublishedPosts()
        {
            // Act
            var result = await _blogService.GetAllPostsAsync(includeUnpublished: false);

            // Assert
            Assert.Single(result);
            Assert.All(result, post => Assert.True(post.IsPublished));
        }

        [Fact]
        public async Task GetAllPostsAsync_WithIncludeUnpublished_ReturnsAllPosts()
        {
            // Act
            var result = await _blogService.GetAllPostsAsync(includeUnpublished: true);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetPostBySlugAsync_WithValidSlug_ReturnsPost()
        {
            // Act
            var result = await _blogService.GetPostBySlugAsync("published-post");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Published Post", result.Title);
            Assert.Equal("published-post", result.Slug);
        }

        [Fact]
        public async Task GetPostBySlugAsync_WithInvalidSlug_ReturnsNull()
        {
            // Act
            var result = await _blogService.GetPostBySlugAsync("non-existent-post");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreatePostAsync_WithValidPost_CreatesPostSuccessfully()
        {
            // Arrange
            var newPost = new BlogPost
            {
                Title = "New Test Post",
                Content = "New test content",
                Summary = "New test summary",
                IsPublished = true,
                CategoryId = 1,
                AuthorId = "test-user-id"
            };

            var tags = new[] { "tag1", "tag2" };
            var mockTags = new List<Tag>
            {
                new Tag { Id = 1, Name = "tag1", Slug = "tag1" },
                new Tag { Id = 2, Name = "tag2", Slug = "tag2" }
            };

            _tagServiceMock.Setup(x => x.GetOrCreateTagsByNamesAsync(tags))
                          .ReturnsAsync(mockTags);

            // Act
            var result = await _blogService.CreatePostAsync(newPost, tags);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Slug);
            Assert.True(result.Id > 0);
            Assert.NotNull(result.PublishedAt);
            Assert.Equal(2, result.BlogPostTags.Count);
        }

        [Fact]
        public async Task IncrementViewCountAsync_WithValidPostId_IncrementsViewCount()
        {
            // Arrange
            var initialViewCount = _context.BlogPosts.First(p => p.Id == 1).ViewCount;

            // Act
            await _blogService.IncrementViewCountAsync(1);

            // Assert
            var updatedPost = await _context.BlogPosts.FindAsync(1);
            Assert.Equal(initialViewCount + 1, updatedPost!.ViewCount);
        }

        [Fact]
        public async Task GetPublishedPostsCountAsync_ReturnsCorrectCount()
        {
            // Act
            var result = await _blogService.GetPublishedPostsCountAsync();

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task GetDraftPostsCountAsync_ReturnsCorrectCount()
        {
            // Act
            var result = await _blogService.GetDraftPostsCountAsync();

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task SearchPostsAsync_WithMatchingTerm_ReturnsMatchingPosts()
        {
            // Act
            var result = await _blogService.SearchPostsAsync("Published");

            // Assert
            Assert.Single(result);
            Assert.Contains("Published", result.First().Title);
        }

        [Fact]
        public async Task SearchPostsAsync_WithNonMatchingTerm_ReturnsEmptyCollection()
        {
            // Act
            var result = await _blogService.SearchPostsAsync("NonExistentTerm");

            // Assert
            Assert.Empty(result);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
