using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechBlog.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRecaptchaSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecaptchaSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SecretKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EnableForLogin = table.Column<bool>(type: "bit", nullable: false),
                    EnableForRegistration = table.Column<bool>(type: "bit", nullable: false),
                    EnableForComments = table.Column<bool>(type: "bit", nullable: false),
                    ScoreThreshold = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecaptchaSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "RecaptchaSettings",
                columns: new[] { "Id", "CreatedAt", "EnableForComments", "EnableForLogin", "EnableForRegistration", "IsEnabled", "ScoreThreshold", "SecretKey", "SiteKey", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2025, 10, 13, 19, 55, 55, 488, DateTimeKind.Utc).AddTicks(6794), false, false, false, false, 0.5f, "", "", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecaptchaSettings");
        }
    }
}
