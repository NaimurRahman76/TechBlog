using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechBlog.Core.Entities;
using TechBlog.Infrastructure.Data;
using TechBlog.Infrastructure.Services;
using Xunit;

namespace TechBlog.Tests.Unit.Services
{
    public class LoggingServiceTests
    {
        private static ApplicationDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var ctx = new ApplicationDbContext(options);
            return ctx;
        }

        [Fact]
        public async Task AddInfo_AddWarning_AddError_ShouldPersistEntries()
        {
            using var db = CreateInMemoryDb();
            var svc = new LoggingService(db);

            await svc.AddInfoAsync("info message", source: "UnitTest", userId: "U1", ip: "127.0.0.1", userAgent: "x");
            await svc.AddWarningAsync("warning message", source: "UnitTest");
            await svc.AddErrorAsync("error message", source: "UnitTest", ex: new InvalidOperationException("boom"));

            var count = await db.LogEntries.CountAsync();
            count.Should().Be(3);

            var errors = await svc.GetLogsAsync(level: "Error");
            errors.Should().HaveCount(1);
            errors.First().Message.Should().Be("error message");
        }

        [Fact]
        public async Task GetLogs_FilterAndSearch_ShouldWork()
        {
            using var db = CreateInMemoryDb();
            var svc = new LoggingService(db);

            await svc.AddInfoAsync("alpha", source: "S1");
            await svc.AddWarningAsync("beta", source: "S2");
            await svc.AddErrorAsync("gamma", source: "S3");

            var page = await svc.GetLogsAsync(search: "a", page: 1, pageSize: 2);
            page.Should().HaveCount(2);
            (await svc.GetLogsCountAsync(search: "a")).Should().Be(3);

            var warnings = await svc.GetLogsAsync(level: "Warning");
            warnings.Should().OnlyContain(l => l.Level == "Warning");
        }

        [Fact]
        public async Task Clear_ByLevel_And_All_ShouldDelete()
        {
            using var db = CreateInMemoryDb();
            var svc = new LoggingService(db);

            await svc.AddInfoAsync("i1");
            await svc.AddErrorAsync("e1");
            await svc.AddErrorAsync("e2");

            await svc.ClearAsync("Error");
            (await db.LogEntries.CountAsync(l => l.Level == "Error")).Should().Be(0);
            (await db.LogEntries.CountAsync()).Should().Be(1);

            await svc.ClearAsync(null);
            (await db.LogEntries.CountAsync()).Should().Be(0);
        }
    }
}
