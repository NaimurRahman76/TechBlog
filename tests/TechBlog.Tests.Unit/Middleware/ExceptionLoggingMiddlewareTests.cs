using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using TechBlog.Core.Interfaces;
using TechBlog.Web.Middleware;
using Xunit;

namespace TechBlog.Tests.Unit.Middleware
{
    public class ExceptionLoggingMiddlewareTests
    {
        [Fact]
        public async Task Invoke_WhenNextThrows_LogsAndRethrows()
        {
            // Arrange
            var thrown = new InvalidOperationException("boom");
            RequestDelegate next = (ctx) => throw thrown;
            var mw = new ExceptionLoggingMiddleware(next);

            var logMock = new Mock<ILoggingService>();
            logMock
                .Setup(l => l.AddErrorAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Exception?>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var ctx = new DefaultHttpContext();
            ctx.Request.Method = "GET";
            ctx.Request.Path = "/test";

            // Act
            Func<Task> act = async () => await mw.Invoke(ctx, logMock.Object);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
            logMock.Verify(l => l.AddErrorAsync(
                It.Is<string>(s => s.Contains("Unhandled exception")),
                nameof(ExceptionLoggingMiddleware),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.Is<Exception?>(e => e == thrown)
            ), Times.Once);
        }
    }
}
