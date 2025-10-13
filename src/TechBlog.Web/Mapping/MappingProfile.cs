using AutoMapper;
using TechBlog.Core.DTOs;
using TechBlog.Core.Entities;

namespace TechBlog.Web.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Category mappings
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.PostsCount, opt => opt.MapFrom(src => src.BlogPosts != null ? src.BlogPosts.Count : 0));

            // Add other mappings as needed
        }
    }
}
