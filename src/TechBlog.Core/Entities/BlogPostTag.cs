namespace TechBlog.Core.Entities
{
    public class BlogPostTag
    {
        public int BlogPostId { get; set; }
        public int TagId { get; set; }
        
        // Navigation properties
        public virtual BlogPost BlogPost { get; set; }
        public virtual Tag Tag { get; set; }
    }
}
