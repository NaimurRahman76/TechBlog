using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;
using TechBlog.Infrastructure.Data;
using TechBlog.Infrastructure.Services;
using TechBlog.Infrastructure.Mappings;
using TechBlog.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Configure DbContext with SQL Server LocalDB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("TechBlog.Web")));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Add application services
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddScoped<IRecaptchaService, RecaptchaService>();
builder.Services.AddHttpClient(); // Required for RecaptchaService

// Add WorkContext and related services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IWorkContext, WorkContext>();

// Add memory cache
builder.Services.AddMemoryCache();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfiles));

// Add controllers with views
builder.Services.AddControllersWithViews();

// Add Razor Pages for Identity
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        var pending = await context.Database.GetPendingMigrationsAsync();
        var applied = await context.Database.GetAppliedMigrationsAsync();

        if (!applied.Any() && !pending.Any())
        {
            // No migrations exist at all (e.g., migration files missing). EnsureCreated as a fallback.
            logger.LogWarning("No EF Core migrations found (applied or pending). Calling EnsureCreated to create schema from the model. Consider adding an InitialCreate migration for proper migration history.");
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            if (pending.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations: {Migrations}", pending.Count(), string.Join(", ", pending));
            }
            else
            {
                logger.LogInformation("No pending migrations detected.");
            }
            await context.Database.MigrateAsync();
        }

        // Seed initial data
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await SeedData.InitializeAsync(services, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

// Global exception logging middleware (custom)
app.UseMiddleware<TechBlog.Web.Middleware.ExceptionLoggingMiddleware>();

// Add simple request logging to console
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Handling request: {Method} {Path}", 
        context.Request.Method, context.Request.Path);
    
    await next();
    
    logger.LogInformation("Finished handling request: {Method} {Path} - {StatusCode}",
        context.Request.Method, context.Request.Path, context.Response.StatusCode);
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map Razor Pages (for Identity)
app.MapRazorPages();

// Map controllers with areas
app.MapAreaControllerRoute(
    name: "Admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

// Specific route for blog posts
app.MapControllerRoute(
    name: "blogPost",
    pattern: "blog/{slug}",
    defaults: new { controller = "Blog", action = "Post" });

// Specific route for blog categories
app.MapControllerRoute(
    name: "blogCategory",
    pattern: "category/{slug}",
    defaults: new { controller = "Blog", action = "Category" });

// Specific route for blog tags
app.MapControllerRoute(
    name: "blogTag",
    pattern: "tag/{slug}",
    defaults: new { controller = "Blog", action = "Tag" });

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Make Program class accessible for testing
public partial class Program { }
