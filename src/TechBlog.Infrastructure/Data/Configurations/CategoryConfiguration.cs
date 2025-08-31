using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechBlog.Core.Entities;

namespace TechBlog.Infrastructure.Data.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(c => c.Id);
            
            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(c => c.Description)
                .HasMaxLength(500);
                
            builder.Property(c => c.Slug)
                .HasMaxLength(100);
                
            // Indexes
            builder.HasIndex(c => c.Name)
                .IsUnique();
                
            builder.HasIndex(c => c.Slug)
                .IsUnique();
                
            // Query filter to exclude soft-deleted entities
            builder.HasQueryFilter(c => !c.IsDeleted);
        }
    }
}
