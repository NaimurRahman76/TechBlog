using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;
using TechBlog.Web.Areas.Admin.Controllers;
using Xunit;

namespace TechBlog.Tests.Unit.Controllers
{
    public class LogsControllerTests
    {
        [Fact]
        public async Task Index_WithoutSource_ReturnsViewWithResults()
        {
            // Arrange
            var logs = new List<LogEntry>
            {
                new LogEntry { Id = 1, Level = "Info", Source = "App", Message = "m1", CreatedAt = DateTime.UtcNow }
            };
            var mockSvc = new Mock<ILoggingService>();
            mockSvc.Setup(s => s.GetLogsAsync(null, null, 1, 50, null, null, null, null))
                   .ReturnsAsync(logs);
            mockSvc.Setup(s => s.GetLogsCountAsync(null, null, null, null, null, null))
                   .ReturnsAsync(1);

            var controller = new LogsController(mockSvc.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var view = result as ViewResult;
            view.Should().NotBeNull();
            view!.Model.Should().BeEquivalentTo(logs);
            ((int)controller.ViewBag.Total).Should().Be(1);
        }
    }
}
