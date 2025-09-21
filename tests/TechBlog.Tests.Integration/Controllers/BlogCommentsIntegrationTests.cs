using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TechBlog.Infrastructure.Data;
using TechBlog.Core.Entities;
using Xunit;

namespace TechBlog.Tests.Integration.Controllers
{
    public class BlogCommentsIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        public BlogCommentsIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CommentsPartial_ReturnsOnlyApproved_TopLevelPaged()
        {
            // Arrange - seed data
            int postId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var user = new ApplicationUser { Id = "u1", UserName = "admin", Email = "a@b.com" };
                db.Users.Add(user);
                var category = new Category { Name = "Cat", Slug = "cat", Description = "" };
                db.Categories.Add(category);
                db.SaveChanges();

                var post = new BlogPost { Title = "Post", Slug = "post", Content = "x", Summary = "sum", CreatedAt = DateTime.UtcNow, IsPublished = true, AuthorId = user.Id, CategoryId = category.Id };
                db.BlogPosts.Add(post);
                db.SaveChanges();
                postId = post.Id;

                // 8 top-level comments, every 2nd unapproved
                for (int i = 0; i < 8; i++)
                {
                    db.Comments.Add(new Comment
                    {
                        BlogPostId = post.Id,
                        Content = $"Top {i}",
                        AuthorName = "User",
                        AuthorEmail = "u@example.com",
                        AuthorId = user.Id,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                        IsApproved = i % 2 == 1
                    });
                }
                db.SaveChanges();
            }

            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Act
            var resp = await client.GetAsync($"/Blog/CommentsPartial?postId={postId}&page=1&pageSize=5");
            resp.EnsureSuccessStatusCode();
            var html = await resp.Content.ReadAsStringAsync();

            // Assert: only approved top-level comments rendered (should be 4 approved out of 8)
            var topLevelCount = Regex.Matches(html, "top-level-comment").Count;
            topLevelCount.Should().BeLessOrEqualTo(5); // page size upper bound
            topLevelCount.Should().Be(4); // 4 approved

            // has-more should be false because page size 5 and only 4 approved
            html.Should().Contain("data-has-more=\"false\"");
        }

        [Fact]
        public async Task RepliesPartial_PagedAndHasMore()
        {
            int parentId;
            int repliesPostId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var user = new ApplicationUser { Id = "u1", UserName = "admin", Email = "a@b.com" };
                db.Users.Add(user);
                var category = new Category { Name = "Cat", Slug = "cat", Description = "" };
                db.Categories.Add(category);
                db.SaveChanges();

                var post = new BlogPost { Title = "Post", Slug = "post", Content = "x", Summary = "sum", CreatedAt = DateTime.UtcNow, IsPublished = true, AuthorId = user.Id, CategoryId = category.Id };
                db.BlogPosts.Add(post);
                db.SaveChanges();
                repliesPostId = post.Id;

                var parent = new Comment
                {
                    BlogPostId = repliesPostId,
                    Content = "Parent",
                    AuthorName = "User",
                    AuthorEmail = "u@example.com",
                    AuthorId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsApproved = true
                };
                db.Comments.Add(parent);
                db.SaveChanges();
                parentId = parent.Id;

                for (int i = 0; i < 12; i++)
                {
                    db.Comments.Add(new Comment
                    {
                        BlogPostId = repliesPostId,
                        ParentCommentId = parentId,
                        Content = $"Reply {i}",
                        AuthorName = "User",
                        AuthorEmail = "u@example.com",
                        AuthorId = user.Id,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                        IsApproved = true
                    });
                }
                db.SaveChanges();
            }

            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var resp = await client.GetAsync($"/Blog/RepliesPartial?postId={repliesPostId}&parentCommentId={parentId}&page=1&pageSize=5");
            resp.EnsureSuccessStatusCode();
            var html = await resp.Content.ReadAsStringAsync();

            // Should indicate has-more and next page
            html.Should().Contain("data-has-more=\"true\"");
            html.Should().Contain("data-next-page=\"2\"");
        }
    }
}
