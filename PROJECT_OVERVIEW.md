# TechBlog - Project Overview

## Project Structure

### 1. Core Project (`TechBlog.Core`)
Contains the core domain models, DTOs, and service interfaces.

#### Key Components:
- **Entities**:
  - `BlogPost`: Main blog post entity with properties like Title, Content, FeaturedImageUrl, etc.
  - `Category`: Categories for blog posts
  - `Tag`: Tags for blog posts with many-to-many relationship
  - `Comment`: User comments on blog posts
  - `ApplicationUser`: Extended identity user with additional properties

- **DTOs**:
  - `PostDto`, `CategoryDto`, `TagDto`, `CommentDto`, `UserDto`
  - `BaseDto` as the base class for all DTOs

- **Interfaces**:
  - `IBlogService`, `ICategoryService`, `ITagService`, `ICommentService`, `IUserService`

### 2. Infrastructure Project (`TechBlog.Infrastructure`)
Handles data access and service implementations.

#### Key Components:
- **Data**:
  - `ApplicationDbContext`: Main database context
  - Entity configurations for `BlogPost`, `Category`, and `Tag`
  - `SeedData` for initial database population

- **Services**:
  - `BlogService`: Implements blog post operations
  - `CategoryService`: Manages blog categories
  - `TagService`: Handles post tagging
  - `CommentService`: Manages post comments
  - `UserService`: Handles user-related operations

- **Mappings**:
  - `MappingProfiles`: AutoMapper configurations

### 3. Web Project (`TechBlog.Web`)
ASP.NET Core MVC web application with admin and public areas.

#### Key Areas:
1. **Admin Area**
   - Controllers: `BlogPostsController`, `CategoriesController`, `TagsController`, `CommentsController`, `UsersController`
   - ViewModels for list views and forms
   - Dashboard with statistics

2. **Public Area**
   - `HomeController`: Public blog listing
   - `BlogController`: Individual blog posts and comments
   - ViewModels for public views

3. **Features**:
   - User authentication and authorization
   - Image upload for blog posts
   - Comment system
   - Tagging system
   - Category management
   - Responsive design

### 4. Test Projects
- `TechBlog.Tests.Unit`: Unit tests
- `TechBlog.Tests.Integration`: Integration tests with `CustomWebApplicationFactory`

## Technical Stack
- **Backend**: ASP.NET Core MVC
- **Database**: Entity Framework Core with SQL Server
- **Authentication**: ASP.NET Core Identity
- **Frontend**: 
  - Razor views with Bootstrap
  - Client-side validation
  - AJAX for dynamic content loading
- **File Storage**: Local file system for uploaded images
- **Testing**: xUnit for unit and integration tests

## Key Features
1. **Blog Management**
   - Create, read, update, and delete blog posts
   - Rich text editing
   - Featured images for posts
   - Post status management (Draft/Published)

2. **Category & Tag System**
   - Hierarchical categories
   - Tag cloud
   - Filtering by category/tag

3. **User Management**
   - Role-based access control
   - User registration and authentication
   - Profile management

4. **Comment System**
   - Nested comments
   - Moderation
   - User notifications

## Configuration
- Database connection strings in `appsettings.json`
- File upload settings (path, allowed extensions, max size)
- Email settings for notifications

## Getting Started
1. Clone the repository
2. Update connection strings in `appsettings.json`
3. Run database migrations
4. Seed initial data
5. Run the application

## API Endpoints
- `/api/posts`: Blog post operations
- `/api/categories`: Category management
- `/api/tags`: Tag management
- `/api/comments`: Comment operations

## Security Considerations
- Input validation on all forms
- Anti-forgery tokens
- Role-based authorization
- Secure file upload handling
- Password hashing

## Deployment
- Can be deployed to any Windows/Linux server with .NET 6.0 runtime
- Configured for Docker deployment
- Environment-specific settings support

## Dependencies
- AutoMapper for object mapping
- FluentValidation for model validation
- X.PagedList for pagination
- SendGrid for email notifications
- ImageSharp for image processing

## Known Issues
- Image upload path handling in development vs production
- Comment notification emails in development
- Performance optimizations needed for large datasets

## Future Enhancements
- Full-text search
- Social media integration
- Content scheduling
- Advanced analytics
- API versioning
- Caching layer
