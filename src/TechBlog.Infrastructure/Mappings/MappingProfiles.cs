using System;
using AutoMapper;
using TechBlog.Core.DTOs;
using TechBlog.Core.Entities;

namespace TechBlog.Infrastructure.Mappings
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            // Category mappings
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.PostsCount, opt => 
                    opt.MapFrom(src => src.BlogPosts.Count(bp => !bp.IsDeleted)));

            CreateMap<Category, CategoryDetailDto>()
                .IncludeBase<Category, CategoryDto>();

            CreateMap<CreateCategoryDto, Category>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<UpdateCategoryDto, Category>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<Category, UpdateCategoryDto>();

            // Tag mappings
            CreateMap<Tag, TagDto>()
                .ForMember(dest => dest.PostCount, opt => 
                    opt.MapFrom(src => src.BlogPostTags.Count));

            CreateMap<Tag, TagDetailDto>()
                .IncludeBase<Tag, TagDto>();

            CreateMap<CreateTagDto, Tag>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<UpdateTagDto, Tag>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<Tag, UpdateTagDto>();

            // BlogPost mappings
            CreateMap<BlogPost, PostListDto>()
                .ForMember(dest => dest.AuthorName, opt => 
                    opt.MapFrom(src => src.Author != null ? $"{src.Author.FirstName} {src.Author.LastName}" : null))
                .ForMember(dest => dest.CategoryName, opt => 
                    opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.CategorySlug, opt => 
                    opt.MapFrom(src => src.Category.Slug))
                .ForMember(dest => dest.Tags, opt => 
                    opt.MapFrom(src => src.BlogPostTags.Select(pt => pt.Tag.Name)));

            CreateMap<BlogPost, PostDetailDto>()
                .IncludeBase<BlogPost, PostListDto>()
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
                .ForMember(dest => dest.CategoryId, opt => 
                    opt.MapFrom(src => src.CategoryId))
                .ForMember(dest => dest.AuthorId, opt => 
                    opt.MapFrom(src => src.AuthorId));

            CreateMap<BlogPost, PostAdminListDto>()
                .IncludeBase<BlogPost, PostListDto>()
                .ForMember(dest => dest.Status, opt => 
                    opt.MapFrom(src => src.IsPublished ? "Published" : "Draft"))
                .ForMember(dest => dest.CommentCount, opt => 
                    opt.MapFrom(src => src.Comments.Count));

            CreateMap<BlogPost, UpdatePostDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.Summary, opt => opt.MapFrom(src => src.Summary))
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
                .ForMember(dest => dest.IsPublished, opt => opt.MapFrom(src => src.IsPublished))
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
                .ForMember(dest => dest.FeaturedImageUrl, opt => opt.MapFrom(src => src.FeaturedImageUrl))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.Tags, opt => 
                    opt.MapFrom(src => string.Join(",", src.BlogPostTags.Select(t => t.Tag.Name))));

            CreateMap<CreatePostDto, BlogPost>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.ViewCount, opt => opt.MapFrom(_ => 0));

            CreateMap<UpdatePostDto, BlogPost>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            // Comment mappings
            CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.AuthorName, opt => 
                    opt.MapFrom(src => !string.IsNullOrEmpty(src.AuthorId) ? 
                        $"{src.Author.FirstName} {src.Author.LastName}" : src.AuthorName))
                .ForMember(dest => dest.AuthorEmail, opt => 
                    opt.MapFrom(src => !string.IsNullOrEmpty(src.AuthorId) ? 
                        src.Author.Email : src.AuthorEmail))
                .ForMember(dest => dest.PostTitle, opt => 
                    opt.MapFrom(src => src.BlogPost.Title))
                .ForMember(dest => dest.PostSlug, opt => 
                    opt.MapFrom(src => src.BlogPost.Slug));

            CreateMap<Comment, CommentAdminListDto>()
                .IncludeBase<Comment, CommentDto>()
                .ForMember(dest => dest.ReplyCount, opt => 
                    opt.MapFrom(src => src.Replies.Count));

            CreateMap<CreateCommentDto, Comment>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.IsApproved, opt => opt.MapFrom(_ => false));

            CreateMap<UpdateCommentDto, Comment>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
        }
    }
}
