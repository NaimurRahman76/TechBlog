using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechBlog.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetExpiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PasswordResetLinkExpirationHours",
                table: "EmailSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "EmailSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordResetLinkExpirationHours" },
                values: new object[] { new DateTime(2025, 10, 20, 20, 40, 42, 991, DateTimeKind.Utc).AddTicks(1933), 1 });

            migrationBuilder.UpdateData(
                table: "RecaptchaSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 20, 20, 40, 42, 991, DateTimeKind.Utc).AddTicks(1821));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetLinkExpirationHours",
                table: "EmailSettings");

            migrationBuilder.UpdateData(
                table: "EmailSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 20, 20, 10, 56, 260, DateTimeKind.Utc).AddTicks(5874));

            migrationBuilder.UpdateData(
                table: "RecaptchaSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 20, 20, 10, 56, 260, DateTimeKind.Utc).AddTicks(5766));
        }
    }
}
