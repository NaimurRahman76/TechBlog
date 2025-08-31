using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechBlog.Core.Entities;

namespace TechBlog.Infrastructure.Data.Configurations
{
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.HasKey(t => t.Id);
            
            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(t => t.Slug)
                .HasMaxLength(100);
                
            // Indexes
            builder.HasIndex(t => t.Name)
                .IsUnique();
                
            builder.HasIndex(t => t.Slug)
                .IsUnique();
                
            // Query filter to exclude soft-deleted entities
            builder.HasQueryFilter(t => !t.IsDeleted);
        }
    }
}
