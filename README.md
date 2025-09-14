# TechBlog

A modern, feature-rich blogging platform built with ASP.NET Core MVC.

## 🚀 Features

- **Blog Management**: Create, edit, and manage blog posts with rich text editing
- **Media Support**: Upload and manage featured images for posts
- **Categories & Tags**: Organize content with categories and tags
- **Comments**: Built-in comment system with moderation
- **User Management**: Role-based access control (Admin/Author/User)
- **Responsive Design**: Mobile-friendly interface

## 🛠️ Tech Stack

- **Backend**: ASP.NET Core 6.0
- **Frontend**: Razor Pages, Bootstrap 5, JavaScript
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Deployment**: Docker support included

## 📦 Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [SQL Server 2019+](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or [VS Code](https://code.visualstudio.com/)

## 🚀 Getting Started

1. **Clone the repository**
   ```bash
   git clone https://github.com/NaimurRahman76/TechBlog.git
   cd TechBlog
   ```

2. **Configure the database**
   - Update the connection string in `appsettings.json`
   - Run database migrations:
     ```bash
     dotnet ef database update --project src/TechBlog.Infrastructure --startup-project src/TechBlog.Web
     ```

3. **Run the application**
   ```bash
   cd src/TechBlog.Web
   dotnet run
   ```

4. **Access the application**
   - Website: `https://localhost:5001`
   - Admin Area: `https://localhost:5001/admin`
   - Default Admin Credentials:
     - Email: admin@techblog.com
     - Password: Admin@123

## 📂 Project Structure

```
TechBlog/
├── src/
│   ├── TechBlog.Core/          # Core domain models and interfaces
│   ├── TechBlog.Infrastructure/# Data access and service implementations
│   └── TechBlog.Web/           # Web application (MVC)
└── tests/                      # Unit and integration tests
```

## 🔧 Configuration

Configuration settings can be found in:
- `appsettings.json` - General application settings
- `appsettings.Development.json` - Development-specific settings
- `appsettings.Production.json` - Production settings

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

1. Fork the project
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📧 Contact

For any questions or feedback, please contact [naimurrahmanrony1@gmail.com](mailto:naimurrahmanrony1@gmail.com)

---

<div align="center">
  Made with ❤️ by [Naimur Rahman]
</div>