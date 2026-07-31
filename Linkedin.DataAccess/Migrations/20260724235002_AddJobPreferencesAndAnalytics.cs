using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Linkedin.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPreferencesAndAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "ProfileOptions",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.CreateTable(
                name: "AnalyticsEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    ViewerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TargetUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PostId = table.Column<int>(type: "int", nullable: true),
                    SearchQuery = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticsEvents_AspNetUsers_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnalyticsEvents_AspNetUsers_ViewerUserId",
                        column: x => x.ViewerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnalyticsEvents_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "JobPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JobTitles = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Locations = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WorkplaceTypes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EmploymentTypes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_PostId_EventType_CreatedAt",
                table: "AnalyticsEvents",
                columns: new[] { "PostId", "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_TargetUserId_EventType_CreatedAt",
                table: "AnalyticsEvents",
                columns: new[] { "TargetUserId", "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_ViewerUserId_EventType_CreatedAt",
                table: "AnalyticsEvents",
                columns: new[] { "ViewerUserId", "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobPreferences_UserId",
                table: "JobPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyticsEvents");

            migrationBuilder.DropTable(
                name: "JobPreferences");

            migrationBuilder.InsertData(
                table: "ProfileOptions",
                columns: new[] { "Id", "CreatedByUserId", "IsApproved", "Name", "NormalizedName", "Type" },
                values: new object[,]
                {
                    { 1, null, true, "C#", "C#", 1 },
                    { 2, null, true, "ASP.NET Core", "ASP.NET CORE", 1 },
                    { 3, null, true, "JavaScript", "JAVASCRIPT", 1 },
                    { 4, null, true, "React", "REACT", 1 },
                    { 5, null, true, "SQL", "SQL", 1 },
                    { 6, null, true, "Git", "GIT", 1 },
                    { 7, null, true, "Software Developer", "SOFTWARE DEVELOPER", 2 },
                    { 8, null, true, "Frontend Developer", "FRONTEND DEVELOPER", 2 },
                    { 9, null, true, "Backend Developer", "BACKEND DEVELOPER", 2 },
                    { 10, null, true, "Full-Stack Developer", "FULL-STACK DEVELOPER", 2 },
                    { 11, null, true, "UI/UX Designer", "UI/UX DESIGNER", 2 },
                    { 12, null, true, "Project Manager", "PROJECT MANAGER", 2 },
                    { 13, null, true, "Software Development", "SOFTWARE DEVELOPMENT", 3 },
                    { 14, null, true, "Information Technology", "INFORMATION TECHNOLOGY", 3 },
                    { 15, null, true, "Financial Services", "FINANCIAL SERVICES", 3 },
                    { 16, null, true, "Education", "EDUCATION", 3 },
                    { 17, null, true, "Healthcare", "HEALTHCARE", 3 },
                    { 18, null, true, "Marketing and Advertising", "MARKETING AND ADVERTISING", 3 },
                    { 19, null, true, "Baku, Azerbaijan", "BAKU, AZERBAIJAN", 4 },
                    { 20, null, true, "Ganja, Azerbaijan", "GANJA, AZERBAIJAN", 4 },
                    { 21, null, true, "Sumgait, Azerbaijan", "SUMGAIT, AZERBAIJAN", 4 },
                    { 22, null, true, "Istanbul, Türkiye", "ISTANBUL, TÜRKIYE", 4 },
                    { 23, null, true, "Dubai, United Arab Emirates", "DUBAI, UNITED ARAB EMIRATES", 4 },
                    { 24, null, true, "Remote", "REMOTE", 4 }
                });
        }
    }
}
