using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Linkedin.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPostAiModerationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiModerationCategories",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AiModerationCheckedAt",
                table: "Posts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiModerationReason",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiModerationRiskLevel",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAiFlagged",
                table: "Posts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModerationStatus",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiModerationCategories",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "AiModerationCheckedAt",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "AiModerationReason",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "AiModerationRiskLevel",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "IsAiFlagged",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ModerationStatus",
                table: "Posts");
        }
    }
}
