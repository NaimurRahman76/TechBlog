using Microsoft.Extensions.Caching.Memory;
using TechBlog.Core.Interfaces;

namespace TechBlog.Infrastructure.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;

        public CacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public void ClearAll()
        {
            ( _memoryCache as MemoryCache )?.Compact(1.0);
        }
    }
}
