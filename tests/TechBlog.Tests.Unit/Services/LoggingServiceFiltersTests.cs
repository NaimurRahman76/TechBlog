using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechBlog.Infrastructure.Data;
using TechBlog.Infrastructure.Services;
using Xunit;

namespace TechBlog.Tests.Unit.Services
{
    public class LoggingServiceFiltersTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetSourcesAsync_ReturnsDistinctOrdered()
        {
            using var db = CreateDb();
            var svc = new LoggingService(db);
            await svc.AddInfoAsync("m1", source: "A");
            await svc.AddInfoAsync("m2", source: "C");
            await svc.AddInfoAsync("m3", source: "B");
            await svc.AddInfoAsync("m4", source: "A");

            var sources = await svc.GetSourcesAsync();
            sources.Should().ContainInOrder("A", "B", "C");
        }

        [Fact]
        public async Task GetLogs_FiltersByDateRange_Source_User()
        {
            using var db = CreateDb();
            var svc = new LoggingService(db);

            // Seed controlled times
            var now = DateTime.UtcNow;
            await db.LogEntries.AddRangeAsync(
                new Core.Entities.LogEntry { Level = "Info", Source = "APP", Message = "old", CreatedAt = now.AddDays(-10), UserId = "U1" },
                new Core.Entities.LogEntry { Level = "Info", Source = "APP", Message = "mid", CreatedAt = now.AddDays(-5), UserId = "U2" },
                new Core.Entities.LogEntry { Level = "Error", Source = "WEB", Message = "new", CreatedAt = now.AddDays(-1), UserId = "U1" }
            );
            await db.SaveChangesAsync();

            var from = now.AddDays(-7);
            var to = now;

            var logs = await svc.GetLogsAsync(level: null, search: null, page: 1, pageSize: 50, fromUtc: from, toUtc: to, source: "APP", userId: null);
            logs.Should().HaveCount(1);
            logs.First().Message.Should().Be("mid");

            var logsUser = await svc.GetLogsAsync(level: null, search: null, page: 1, pageSize: 50, fromUtc: from, toUtc: to, source: "WEB", userId: "U1");
            logsUser.Should().HaveCount(1);
            logsUser.First().Message.Should().Be("new");
        }
    }
}
