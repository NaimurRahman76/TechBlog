using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TechBlog.Core.Entities;
using TechBlog.Infrastructure.Data;
using TechBlog.Infrastructure.Services;
using Xunit;

namespace TechBlog.Tests.Unit.Services
{
    public class TagServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<TagService>> _loggerMock;
        private readonly TagService _tagService;

        public TagServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<TagService>>();
            _tagService = new TagService(_context, _loggerMock.Object);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var tags = new List<Tag>
            {
                new Tag
                {
                    Id = 1,
                    Name = "C#",
                    Slug = "csharp",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Tag
                {
                    Id = 2,
                    Name = "ASP.NET",
                    Slug = "aspnet",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                }
            };

            _context.Tags.AddRange(tags);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAllTagsAsync_ReturnsAllTags()
        {
            // Act
            var result = await _tagService.GetAllTagsAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, t => t.Name == "C#");
            Assert.Contains(result, t => t.Name == "ASP.NET");
        }

        [Fact]
        public async Task GetTagByIdAsync_WithValidId_ReturnsTag()
        {
            // Act
            var result = await _tagService.GetTagByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("C#", result.Name);
            Assert.Equal("csharp", result.Slug);
        }

        [Fact]
        public async Task GetTagByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _tagService.GetTagByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTagBySlugAsync_WithValidSlug_ReturnsTag()
        {
            // Act
            var result = await _tagService.GetTagBySlugAsync("csharp");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("C#", result.Name);
            Assert.Equal("csharp", result.Slug);
        }

        [Fact]
        public async Task CreateTagAsync_WithValidTag_CreatesTagSuccessfully()
        {
            // Arrange
            var newTag = new Tag
            {
                Name = "JavaScript"
            };

            // Act
            var result = await _tagService.CreateTagAsync(newTag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("JavaScript", result.Name);
            Assert.NotNull(result.Slug);
            Assert.True(result.Id > 0);
        }

        [Fact]
        public async Task UpdateTagAsync_WithValidTag_UpdatesTagSuccessfully()
        {
            // Arrange
            var existingTag = await _context.Tags.FindAsync(1);
            existingTag!.Name = "C# Updated";

            // Act
            await _tagService.UpdateTagAsync(existingTag);

            // Assert
            var updatedTag = await _context.Tags.FindAsync(1);
            Assert.Equal("C# Updated", updatedTag!.Name);
            Assert.NotNull(updatedTag.UpdatedAt);
        }

        [Fact]
        public async Task GetOrCreateTagsByNamesAsync_WithExistingAndNewTags_ReturnsAllTags()
        {
            // Arrange
            var tagNames = new[] { "C#", "JavaScript", "Python" };

            // Act
            var result = await _tagService.GetOrCreateTagsByNamesAsync(tagNames);

            // Assert
            Assert.Equal(3, result.Count());
            Assert.Contains(result, t => t.Name == "C#");
            Assert.Contains(result, t => t.Name == "JavaScript");
            Assert.Contains(result, t => t.Name == "Python");
        }

        [Fact]
        public async Task GetOrCreateTagsByNamesAsync_WithEmptyArray_ReturnsEmptyCollection()
        {
            // Act
            var result = await _tagService.GetOrCreateTagsByNamesAsync(new string[0]);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetOrCreateTagsByNamesAsync_WithNullArray_ReturnsEmptyCollection()
        {
            // Act
            var result = await _tagService.GetOrCreateTagsByNamesAsync(null!);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task TagSlugExistsAsync_WithExistingSlug_ReturnsTrue()
        {
            // Act
            var result = await _tagService.TagSlugExistsAsync("csharp");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TagSlugExistsAsync_WithNonExistingSlug_ReturnsFalse()
        {
            // Act
            var result = await _tagService.TagSlugExistsAsync("non-existing-slug");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetTotalTagsCountAsync_ReturnsCorrectCount()
        {
            // Act
            var result = await _tagService.GetTotalTagsCountAsync();

            // Assert
            Assert.Equal(2, result);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
