using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TechBlog.Core.Entities;

namespace TechBlog.Infrastructure.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, 
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager)
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("SeedData");
            
            // Create roles if they don't exist
            string[] roleNames = { "Admin", "User" };
            IdentityResult roleResult;

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (!roleResult.Succeeded)
                    {
                        logger.LogError($"Error creating role {roleName}: {string.Join(", ", roleResult.Errors)}");
                    }
                }
            }

            // Create admin user if it doesn't exist
            var adminEmail = configuration["ApplicationSettings:AdminEmail"];
            var adminPassword = configuration["ApplicationSettings:AdminPassword"];
            
            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                throw new InvalidOperationException("Admin email or password not configured in appsettings.json");
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true
                };

                var createAdmin = await userManager.CreateAsync(admin, adminPassword);
                if (createAdmin.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    logger.LogInformation("Admin user created successfully.");
                }
                else
                {
                    logger.LogError($"Error creating admin user: {string.Join(", ", createAdmin.Errors)}");
                }
            }
            else
            {
                // Ensure admin user has the Admin role
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Create some sample categories if none exist
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            if (!await dbContext.Categories.AnyAsync())
            {
                var categories = new[]
                {
                    new Category { Name = "ASP.NET Core", Description = "Articles about ASP.NET Core development", Slug = "aspnet-core" },
                    new Category { Name = "C#", Description = "C# programming language tips and tricks", Slug = "csharp" },
                    new Category { Name = "Entity Framework", Description = "Database access with Entity Framework Core", Slug = "entity-framework" },
                    new Category { Name = "JavaScript", Description = "Frontend development with JavaScript", Slug = "javascript" },
                    new Category { Name = "Electronics", Description = "Electronics and hardware projects", Slug = "electronics" }
                };

                await dbContext.Categories.AddRangeAsync(categories);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Sample categories created successfully.");
            }

            // Create some sample tags if none exist
            if (!await dbContext.Tags.AnyAsync())
            {
                var tags = new[]
                {
                    new Tag { Name = "Tutorial", Slug = "tutorial" },
                    new Tag { Name = "Tips", Slug = "tips" },
                    new Tag { Name = "Performance", Slug = "performance" },
                    new Tag { Name = "Security", Slug = "security" },
                    new Tag { Name = "Beginner", Slug = "beginner" },
                    new Tag { Name = "Advanced", Slug = "advanced" }
                };

                await dbContext.Tags.AddRangeAsync(tags);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Sample tags created successfully.");
            }
        }
    }
}
