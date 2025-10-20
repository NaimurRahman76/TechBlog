using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechBlog.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSettingsAndVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmtpHost = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SmtpPort = table.Column<int>(type: "int", nullable: false),
                    FromEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EnableSsl = table.Column<bool>(type: "bit", nullable: false),
                    EnableEmailVerification = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    VerificationLinkExpirationHours = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "EmailSettings",
                columns: new[] { "Id", "CreatedAt", "EnableEmailVerification", "EnableSsl", "FromEmail", "FromName", "IsEnabled", "Password", "SmtpHost", "SmtpPort", "UpdatedAt", "Username", "VerificationLinkExpirationHours" },
                values: new object[] { 1, new DateTime(2025, 10, 20, 20, 10, 56, 260, DateTimeKind.Utc).AddTicks(5874), true, true, "noreply@techblog.com", "TechBlog", false, "", "smtp.gmail.com", 587, null, "", 24 });

            migrationBuilder.UpdateData(
                table: "RecaptchaSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 20, 20, 10, 56, 260, DateTimeKind.Utc).AddTicks(5766));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSettings");

            migrationBuilder.UpdateData(
                table: "RecaptchaSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 13, 19, 55, 55, 488, DateTimeKind.Utc).AddTicks(6794));
        }
    }
}
