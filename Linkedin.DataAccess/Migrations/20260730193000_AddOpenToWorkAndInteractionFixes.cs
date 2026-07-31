using Linkedin.Core.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Linkedin.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260730193000_AddOpenToWorkAndInteractionFixes")]
    public partial class AddOpenToWorkAndInteractionFixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOpenToWork",
                table: "JobPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OnsiteLocations",
                table: "JobPreferences",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemoteLocations",
                table: "JobPreferences",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartAvailability",
                table: "JobPreferences",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Like rows are removed when unliked, so every existing row
            // represents an active reaction. Repair legacy rows created before
            // the service explicitly set isLiked=true.
            migrationBuilder.Sql(
                "UPDATE [Likes] SET [isLiked] = 1 WHERE [isLiked] = 0;");
            migrationBuilder.Sql(
                "WITH ranked AS (" +
                "SELECT [Id], ROW_NUMBER() OVER (" +
                "PARTITION BY [PostId], [UserId] ORDER BY [Id]) AS rn " +
                "FROM [Likes]) DELETE FROM ranked WHERE rn > 1;");
            migrationBuilder.Sql(
                "UPDATE p SET p.[LikeCount] = " +
                "(SELECT COUNT(*) FROM [Likes] l " +
                "WHERE l.[PostId] = p.[Id] AND l.[isLiked] = 1) " +
                "FROM [Posts] p;");
            migrationBuilder.Sql(
                "UPDATE p SET p.[CommentCount] = " +
                "(SELECT COUNT(*) FROM [Comments] c " +
                "WHERE c.[PostId] = p.[Id]) " +
                "FROM [Posts] p;");

            migrationBuilder.CreateIndex(
                name: "IX_Likes_PostId_UserId",
                table: "Likes",
                columns: new[] { "PostId", "UserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Likes_PostId_UserId",
                table: "Likes");

            migrationBuilder.DropColumn(
                name: "IsOpenToWork",
                table: "JobPreferences");

            migrationBuilder.DropColumn(
                name: "OnsiteLocations",
                table: "JobPreferences");

            migrationBuilder.DropColumn(
                name: "RemoteLocations",
                table: "JobPreferences");

            migrationBuilder.DropColumn(
                name: "StartAvailability",
                table: "JobPreferences");
        }
    }
}
