using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using TechBlog.Core.Entities;

namespace TechBlog.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<BlogPostTag> BlogPostTags { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }
        public DbSet<RecaptchaSettings> RecaptchaSettings { get; set; }
        public DbSet<EmailSettings> EmailSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite key for BlogPostTag
            modelBuilder.Entity<BlogPostTag>()
                .HasKey(bt => new { bt.BlogPostId, bt.TagId });

            // Configure relationships
            modelBuilder.Entity<BlogPost>()
                .HasOne(bp => bp.Category)
                .WithMany(c => c.BlogPosts)
                .HasForeignKey(bp => bp.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BlogPost>()
                .HasOne(bp => bp.Author)
                .WithMany(u => u.BlogPosts)
                .HasForeignKey(bp => bp.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BlogPostTag>()
                .HasOne(bt => bt.BlogPost)
                .WithMany(bp => bp.BlogPostTags)
                .HasForeignKey(bt => bt.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BlogPostTag>()
                .HasOne(bt => bt.Tag)
                .WithMany(t => t.BlogPostTags)
                .HasForeignKey(bt => bt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.BlogPost)
                .WithMany(bp => bp.Comments)
                .HasForeignKey(c => c.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure RecaptchaSettings to have a single row
            modelBuilder.Entity<RecaptchaSettings>()
                .HasData(new RecaptchaSettings
                {
                    Id = 1,
                    SiteKey = "",
                    SecretKey = "",
                    IsEnabled = false,
                    EnableForLogin = false,
                    EnableForRegistration = false,
                    EnableForComments = false,
                    ScoreThreshold = 0.5f,
                    CreatedAt = DateTime.UtcNow
                });

            // Configure EmailSettings to have a single row
            modelBuilder.Entity<EmailSettings>()
                .HasData(new EmailSettings
                {
                    Id = 1,
                    SmtpHost = "smtp.gmail.com",
                    SmtpPort = 587,
                    FromEmail = "noreply@techblog.com",
                    FromName = "TechBlog",
                    Username = "",
                    Password = "",
                    EnableSsl = true,
                    EnableEmailVerification = true,
                    IsEnabled = false,
                    VerificationLinkExpirationHours = 24,
                    CreatedAt = DateTime.UtcNow
                });

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(pc => pc.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Apply configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Logs index for faster querying
            modelBuilder.Entity<LogEntry>()
                .HasIndex(l => l.CreatedAt);
        }
    }
}
