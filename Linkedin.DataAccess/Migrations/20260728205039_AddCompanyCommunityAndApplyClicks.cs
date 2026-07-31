using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Linkedin.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCommunityAndApplyClicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_ReceiverId_EventId_Type",
                table: "Notifications");

            migrationBuilder.AddColumn<int>(
                name: "MentionedCompanyId",
                table: "Posts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobPostId",
                table: "AnalyticsEvents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_MentionedCompanyId_CreatedAt",
                table: "Posts",
                columns: new[] { "MentionedCompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReceiverId",
                table: "Notifications",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SenderId_ReceiverId_EventId_Type",
                table: "Notifications",
                columns: new[] { "SenderId", "ReceiverId", "EventId", "Type" },
                unique: true,
                filter: "[EventId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_JobPostId_EventType_CreatedAt",
                table: "AnalyticsEvents",
                columns: new[] { "JobPostId", "EventType", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_AnalyticsEvents_JobPosts_JobPostId",
                table: "AnalyticsEvents",
                column: "JobPostId",
                principalTable: "JobPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Company_MentionedCompanyId",
                table: "Posts",
                column: "MentionedCompanyId",
                principalTable: "Company",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalyticsEvents_JobPosts_JobPostId",
                table: "AnalyticsEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Company_MentionedCompanyId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_MentionedCompanyId_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ReceiverId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_SenderId_ReceiverId_EventId_Type",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_AnalyticsEvents_JobPostId_EventType_CreatedAt",
                table: "AnalyticsEvents");

            migrationBuilder.DropColumn(
                name: "MentionedCompanyId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "JobPostId",
                table: "AnalyticsEvents");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReceiverId_EventId_Type",
                table: "Notifications",
                columns: new[] { "ReceiverId", "EventId", "Type" },
                unique: true,
                filter: "[EventId] IS NOT NULL");
        }
    }
}
