using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TechBlog.Core.Entities;

namespace TechBlog.Core.Interfaces
{
    public interface ILoggingService
    {
        Task AddInfoAsync(string message, string source = "App", string? userId = null, string? ip = null, string? userAgent = null, Exception? ex = null);
        Task AddWarningAsync(string message, string source = "App", string? userId = null, string? ip = null, string? userAgent = null, Exception? ex = null);
        Task AddErrorAsync(string message, string source = "App", string? userId = null, string? ip = null, string? userAgent = null, Exception? ex = null);
        Task<LogEntry?> GetLogAsync(int id);
        Task<IEnumerable<LogEntry>> GetLogsAsync(string? level = null, string? search = null, int page = 1, int pageSize = 50, DateTime? fromUtc = null, DateTime? toUtc = null, string? source = null, string? userId = null);
        Task<int> GetLogsCountAsync(string? level = null, string? search = null, DateTime? fromUtc = null, DateTime? toUtc = null, string? source = null, string? userId = null);
        Task<IEnumerable<string>> GetSourcesAsync();
        Task ClearAsync(string? level = null);
    }
}
