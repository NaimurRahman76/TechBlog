using System;
using System.Collections.Generic;
using System.Linq;
using TechBlog.Web.Extensions;
using Xunit;

namespace TechBlog.Tests.Unit.Extensions
{
    public class PagedListExtensionsTests
    {
        [Fact]
        public void ToPagedList_WithQueryable_ShouldReturnPagedList()
        {
            // Arrange
            var items = new List<string> { "Item1", "Item2", "Item3", "Item4", "Item5" }.AsQueryable();
            var pageIndex = 1;
            var pageSize = 2;

            // Act
            var result = items.ToPagedList(pageIndex, pageSize);

            // Assert
            Assert.Equal(pageIndex, result.PageIndex);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(3, result.TotalPages);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal("Item1", result.FirstOrDefault());
            Assert.Equal("Item2", result.FirstOrDefault());
            Assert.True(result.HasPreviousPage);
            Assert.False(result.HasNextPage);
        }

        [Fact]
        public void ToPagedList_WithEnumerable_ShouldReturnPagedList()
        {
            // Arrange
            var items = new List<string> { "Item1", "Item2", "Item3", "Item4", "Item5" };
            var pageIndex = 2;
            var pageSize = 2;

            // Act
            var result = items.ToPagedList(pageIndex, pageSize);

            // Assert
            Assert.Equal(pageIndex, result.PageIndex);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(3, result.TotalPages);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal("Item3", result.FirstOrDefault());
            Assert.Equal("Item4", result.FirstOrDefault());
            Assert.True(result.HasPreviousPage);
            Assert.True(result.HasNextPage);
        }

        [Fact]
        public void ToPagedList_WithPageIndexLessThanOne_ShouldHandleCorrectly()
        {
            // Arrange
            var items = new List<string> { "Item1", "Item2", "Item3" }.AsQueryable();
            var pageIndex = 0;
            var pageSize = 2;

            // Act
            var result = items.ToPagedList(pageIndex, pageSize);

            // Assert
            Assert.Equal(1, result.PageIndex); // Should default to 1
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(2, result.TotalPages);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public void ToPagedList_WithEmptyCollection_ShouldReturnEmptyPagedList()
        {
            // Arrange
            var items = new List<string>().AsQueryable();
            var pageIndex = 1;
            var pageSize = 10;

            // Act
            var result = items.ToPagedList(pageIndex, pageSize);

            // Assert
            Assert.Equal(pageIndex, result.PageIndex);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(0, result.TotalCount);
            Assert.Equal(0, result.TotalPages);
            Assert.Equal(0, result.TotalCount);
            Assert.False(result.HasPreviousPage);
            Assert.False(result.HasNextPage);
        }

        [Fact]
        public void ToPagedList_WithLastPage_ShouldReturnCorrectItems()
        {
            // Arrange
            var items = new List<string> { "Item1", "Item2", "Item3", "Item4", "Item5" }.AsQueryable();
            var pageIndex = 3;
            var pageSize = 2;

            // Act
            var result = items.ToPagedList(pageIndex, pageSize);

            // Assert
            Assert.Equal(pageIndex, result.PageIndex);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(3, result.TotalPages);
            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Item5", result.FirstOrDefault());
            Assert.True(result.HasPreviousPage);
            Assert.False(result.HasNextPage);
        }
    }
}
