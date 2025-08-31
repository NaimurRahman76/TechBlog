using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechBlog.Core.Entities;

namespace TechBlog.Infrastructure.Data.Configurations
{
    public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
    {
        public void Configure(EntityTypeBuilder<BlogPost> builder)
        {
            builder.HasKey(bp => bp.Id);
            
            builder.Property(bp => bp.Title)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(bp => bp.Slug)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(bp => bp.Content)
                .IsRequired();
                
            builder.Property(bp => bp.Summary)
                .HasMaxLength(500);
                
            builder.Property(bp => bp.ViewCount)
                .HasDefaultValue(0);
                
            builder.Property(bp => bp.IsPublished)
                .HasDefaultValue(false);
                
            // Indexes
            builder.HasIndex(bp => bp.Slug)
                .IsUnique();
                
            builder.HasIndex(bp => bp.IsPublished);
            
            builder.HasIndex(bp => bp.PublishedAt);
            
            // Query filter to exclude soft-deleted entities
            builder.HasQueryFilter(bp => !bp.IsDeleted);
        }
    }
}
