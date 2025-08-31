using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechBlog.Core.Entities;
using TechBlog.Infrastructure.Data;

namespace TechBlog.Infrastructure.Services
{
    public abstract class BaseService
    {
        protected readonly ApplicationDbContext _context;
        protected readonly ILogger _logger;

        protected BaseService(ApplicationDbContext context, ILogger logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected async Task<bool> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync() > 0;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "A concurrency error occurred while saving changes.");
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "An error occurred while updating the database.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while saving changes.");
                throw;
            }
        }
    }
}
