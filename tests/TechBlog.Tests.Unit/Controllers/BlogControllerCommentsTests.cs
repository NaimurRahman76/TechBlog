using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TechBlog.Core.DTOs;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;
using TechBlog.Web.Controllers;
using Xunit;

namespace TechBlog.Tests.Unit.Controllers
{
    public class BlogControllerCommentsTests
    {
        private static BlogController CreateController(
            Mock<ICommentService> commentService,
            Mock<IWorkContext> workContext)
        {
            var blogSvc = new Mock<IBlogService>();
            var catSvc = new Mock<ICategoryService>();
            var tagSvc = new Mock<ITagService>();
            var mapper = new Mock<AutoMapper.IMapper>();
            var logger = new Mock<ILogger<BlogController>>();

            return new BlogController(
                blogSvc.Object,
                catSvc.Object,
                tagSvc.Object,
                commentService.Object,
                workContext.Object,
                mapper.Object,
                logger.Object);
        }

        private static List<Comment> MakeComments(int postId, int topLevelCount, int repliesPerTop, int unapprovedEvery = 0)
        {
            var list = new List<Comment>();
            int id = 1;
            for (int i = 0; i < topLevelCount; i++)
            {
                var top = new Comment
                {
                    Id = id++,
                    BlogPostId = postId,
                    Content = $"Top {i}",
                    AuthorName = "User",
                    AuthorEmail = "u@example.com",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                    IsApproved = (unapprovedEvery == 0) || (i % unapprovedEvery != 0)
                };
                list.Add(top);
                for (int r = 0; r < repliesPerTop; r++)
                {
                    list.Add(new Comment
                    {
                        Id = id++,
                        BlogPostId = postId,
                        Content = $"Reply {i}-{r}",
                        AuthorName = "User",
                        AuthorEmail = "u@example.com",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-(i*10 + r)),
                        ParentCommentId = top.Id,
                        IsApproved = true
                    });
                }
            }
            return list;
        }

        [Fact]
        public async Task CommentsPartial_PublicOnlyShowsApproved_AndPagesTopLevel()
        {
            // Arrange
            int postId = 100;
            var comments = MakeComments(postId, topLevelCount: 10, repliesPerTop: 2, unapprovedEvery: 0);
            var commentSvc = new Mock<ICommentService>();
            // includeUnapproved=false returns only approved
            commentSvc.Setup(s => s.GetCommentsByPostIdAsync(postId, false))
                      .ReturnsAsync(comments.Where(c => c.IsApproved && c.BlogPostId == postId));
            // includeUnapproved=true returns all (only when admin/author)
            commentSvc.Setup(s => s.GetCommentsByPostIdAsync(postId, true))
                      .ReturnsAsync(comments);

            var workCtx = new Mock<IWorkContext>();
            workCtx.SetupGet(w => w.IsAdmin).Returns(false);
            workCtx.SetupGet(w => w.IsAuthor).Returns(false);

            var controller = CreateController(commentSvc, workCtx);

            // Act
            var result = await controller.CommentsPartial(postId, page: 1, pageSize: 5, replyPreviewSize: 5) as PartialViewResult;

            // Assert
            result.Should().NotBeNull();
            result!.ViewName.Should().Be("_CommentsPage");
            var model = result.Model as IEnumerable<CommentDto>;
            model.Should().NotBeNull();
            model!.Count().Should().Be(5); // pageSize
            ((bool)controller.ViewBag.HasMore).Should().BeTrue();
            ((int)controller.ViewBag.NextPage).Should().Be(2);
        }

        [Fact]
        public async Task RepliesPartial_LoadsAllRepliesForParent()
        {
            // Arrange
            int postId = 200;
            var comments = MakeComments(postId, topLevelCount: 1, repliesPerTop: 6);
            var parentId = comments.First(c => c.ParentCommentId == null).Id;

            var commentSvc = new Mock<ICommentService>();
            commentSvc.Setup(s => s.GetCommentsByPostIdAsync(postId, false))
                      .ReturnsAsync(comments.Where(c => c.IsApproved));

            var workCtx = new Mock<IWorkContext>();
            workCtx.SetupGet(w => w.IsAdmin).Returns(false);
            workCtx.SetupGet(w => w.IsAuthor).Returns(false);

            var controller = CreateController(commentSvc, workCtx);

            // Act
            var result = await controller.RepliesPartial(postId, parentId, page: 1, pageSize: 10) as PartialViewResult;

            // Assert
            result.Should().NotBeNull();
            var model = result!.Model as IEnumerable<CommentDto>;
            model.Should().NotBeNull();
            model!.Count().Should().Be(6);
        }
    }
}
