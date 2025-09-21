using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;
using TechBlog.Infrastructure.Data;

namespace TechBlog.Infrastructure.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly ApplicationDbContext _db;

        public LoggingService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task AddInfoAsync(string message, string source = "App", string? userId = null, string? ip = null, string? userAgent = null, Exception? ex = null)
            => await AddAsync("Info", message, source, userId, ip, userAgent, ex);

        public async Task AddWarningAsync(string message, string source = "App", string? userId = null, string? ip = null, string? userAgent = null, Exception? ex = null)
            => await AddAsync("Warning", message, source, userId, ip, userAgent, ex);

        public async Task AddErrorAsync(string message, string source = "App", string? userId = null, string? ip = null, string? userAgent = null, Exception? ex = null)
            => await AddAsync("Error", message, source, userId, ip, userAgent, ex);

        private async Task AddAsync(string level, string message, string source, string? userId, string? ip, string? userAgent, Exception? ex)
        {
            var entry = new LogEntry
            {
                Level = level,
                Source = source,
                Message = message,
                Exception = ex?.ToString(),
                UserId = userId,
                IpAddress = ip,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };
            _db.LogEntries.Add(entry);
            await _db.SaveChangesAsync();
        }

        public async Task<LogEntry?> GetLogAsync(int id)
        {
            return await _db.LogEntries.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<IEnumerable<LogEntry>> GetLogsAsync(string? level = null, string? search = null, int page = 1, int pageSize = 50, DateTime? fromUtc = null, DateTime? toUtc = null, string? source = null, string? userId = null)
        {
            var q = _db.LogEntries.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(level))
                q = q.Where(l => l.Level == level);
            if (fromUtc.HasValue)
                q = q.Where(l => l.CreatedAt >= fromUtc.Value);
            if (toUtc.HasValue)
                q = q.Where(l => l.CreatedAt <= toUtc.Value);
            if (!string.IsNullOrWhiteSpace(source))
                q = q.Where(l => l.Source == source);
            if (!string.IsNullOrWhiteSpace(userId))
                q = q.Where(l => l.UserId == userId);
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                q = q.Where(l => l.Message.ToLower().Contains(search) || (l.Source != null && l.Source.ToLower().Contains(search)) || (l.Exception != null && l.Exception.ToLower().Contains(search)));
            }
            q = q.OrderByDescending(l => l.CreatedAt);
            return await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<int> GetLogsCountAsync(string? level = null, string? search = null, DateTime? fromUtc = null, DateTime? toUtc = null, string? source = null, string? userId = null)
        {
            var q = _db.LogEntries.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(level))
                q = q.Where(l => l.Level == level);
            if (fromUtc.HasValue)
                q = q.Where(l => l.CreatedAt >= fromUtc.Value);
            if (toUtc.HasValue)
                q = q.Where(l => l.CreatedAt <= toUtc.Value);
            if (!string.IsNullOrWhiteSpace(source))
                q = q.Where(l => l.Source == source);
            if (!string.IsNullOrWhiteSpace(userId))
                q = q.Where(l => l.UserId == userId);
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                q = q.Where(l => l.Message.ToLower().Contains(search) || (l.Source != null && l.Source.ToLower().Contains(search)) || (l.Exception != null && l.Exception.ToLower().Contains(search)));
            }
            return await q.CountAsync();
        }

        public async Task<IEnumerable<string>> GetSourcesAsync()
        {
            return await _db.LogEntries
                .AsNoTracking()
                .Where(l => l.Source != null && l.Source != "")
                .Select(l => l.Source!)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
        }

        public async Task ClearAsync(string? level = null)
        {
            if (string.IsNullOrWhiteSpace(level))
            {
                var all = await _db.LogEntries.ToListAsync();
                _db.LogEntries.RemoveRange(all);
            }
            else
            {
                var byLevel = await _db.LogEntries.Where(l => l.Level == level).ToListAsync();
                _db.LogEntries.RemoveRange(byLevel);
            }

            await _db.SaveChangesAsync();
        }
    }
}
