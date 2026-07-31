using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Linkedin.Core.Data;

#nullable disable

namespace Linkedin.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260729043000_AddCompanyTalentWorkspace")]
    public partial class AddCompanyTalentWorkspace : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinimumExperienceYears",
                table: "JobPosts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RequiredSkills",
                table: "JobPosts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobPostId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SavedTalents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CandidateId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedTalents", item => item.Id);
                    table.ForeignKey(
                        name: "FK_SavedTalents_AspNetUsers_CandidateId",
                        column: item => item.CandidateId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SavedTalents_AspNetUsers_EmployerId",
                        column: item => item.EmployerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobPostId = table.Column<int>(type: "int", nullable: false),
                    EmployerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CandidateId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobInvitations", item => item.Id);
                    table.ForeignKey(
                        name: "FK_JobInvitations_AspNetUsers_CandidateId",
                        column: item => item.CandidateId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobInvitations_AspNetUsers_EmployerId",
                        column: item => item.EmployerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobInvitations_JobPosts_JobPostId",
                        column: item => item.JobPostId,
                        principalTable: "JobPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_JobPostId",
                table: "Notifications",
                column: "JobPostId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTalents_CandidateId",
                table: "SavedTalents",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTalents_EmployerId_CandidateId",
                table: "SavedTalents",
                columns: new[] { "EmployerId", "CandidateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobInvitations_CandidateId",
                table: "JobInvitations",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_JobInvitations_EmployerId",
                table: "JobInvitations",
                column: "EmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_JobInvitations_JobPostId_EmployerId_CandidateId",
                table: "JobInvitations",
                columns: new[] { "JobPostId", "EmployerId", "CandidateId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_JobPosts_JobPostId",
                table: "Notifications",
                column: "JobPostId",
                principalTable: "JobPosts",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_JobPosts_JobPostId",
                table: "Notifications");

            migrationBuilder.DropTable(name: "JobInvitations");
            migrationBuilder.DropTable(name: "SavedTalents");
            migrationBuilder.DropIndex(name: "IX_Notifications_JobPostId", table: "Notifications");
            migrationBuilder.DropColumn(name: "JobPostId", table: "Notifications");
            migrationBuilder.DropColumn(name: "MinimumExperienceYears", table: "JobPosts");
            migrationBuilder.DropColumn(name: "RequiredSkills", table: "JobPosts");
        }
    }
}
