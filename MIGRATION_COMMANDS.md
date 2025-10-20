# Database Migration Commands

## Create and Apply Email Settings Migration

To add the email validation feature to your database, run the following commands:

### 1. Create Migration

```powershell
# From the solution root directory
dotnet ef migrations add AddEmailSettingsAndVerification --project src/TechBlog.Web --startup-project src/TechBlog.Web
```

### 2. Apply Migration

```powershell
# Apply to database
dotnet ef database update --project src/TechBlog.Web --startup-project src/TechBlog.Web
```

### 3. Verify Migration

```powershell
# List all migrations
dotnet ef migrations list --project src/TechBlog.Web
```

## What This Migration Adds

The migration creates the following:

### EmailSettings Table
- `Id` (int, primary key)
- `SmtpHost` (nvarchar(200))
- `SmtpPort` (int)
- `FromEmail` (nvarchar(200))
- `FromName` (nvarchar(200))
- `Username` (nvarchar(200))
- `Password` (nvarchar(200))
- `EnableSsl` (bit)
- `EnableEmailVerification` (bit)
- `IsEnabled` (bit)
- `VerificationLinkExpirationHours` (int)
- `CreatedAt` (datetime2)
- `UpdatedAt` (datetime2, nullable)

### Seed Data
- Default EmailSettings record with ID = 1
- Pre-configured with Gmail SMTP settings
- Email verification enabled by default
- Service disabled by default (requires admin configuration)

## Rollback Migration (if needed)

```powershell
# Remove last migration
dotnet ef migrations remove --project src/TechBlog.Web

# Rollback to specific migration
dotnet ef database update <PreviousMigrationName> --project src/TechBlog.Web
```

## Production Deployment

For production environments:

```powershell
# Generate SQL script instead of direct update
dotnet ef migrations script --project src/TechBlog.Web --output migration.sql

# Review the SQL script before applying to production database
```

## Troubleshooting

### Error: "Build failed"
- Ensure all projects compile successfully
- Run `dotnet build` first

### Error: "No DbContext found"
- Verify you're in the correct directory
- Check the project path in the command

### Error: "Migration already exists"
- Choose a different migration name
- Or remove the existing migration first

## Additional Commands

```powershell
# View pending migrations
dotnet ef migrations list --project src/TechBlog.Web

# Generate SQL script for specific migration
dotnet ef migrations script <FromMigration> <ToMigration> --project src/TechBlog.Web

# Drop database (CAUTION: Deletes all data)
dotnet ef database drop --project src/TechBlog.Web
```
