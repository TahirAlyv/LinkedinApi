using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Linkedin.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileOptionsAndOrganizationLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Experience",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstitutionCompanyId",
                table: "Education",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProfileOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileOptions", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Experience_CompanyId",
                table: "Experience",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Education_InstitutionCompanyId",
                table: "Education",
                column: "InstitutionCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileOptions_Type_NormalizedName",
                table: "ProfileOptions",
                columns: new[] { "Type", "NormalizedName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Education_Company_InstitutionCompanyId",
                table: "Education",
                column: "InstitutionCompanyId",
                principalTable: "Company",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Experience_Company_CompanyId",
                table: "Experience",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Education_Company_InstitutionCompanyId",
                table: "Education");

            migrationBuilder.DropForeignKey(
                name: "FK_Experience_Company_CompanyId",
                table: "Experience");

            migrationBuilder.DropTable(
                name: "ProfileOptions");

            migrationBuilder.DropIndex(
                name: "IX_Experience_CompanyId",
                table: "Experience");

            migrationBuilder.DropIndex(
                name: "IX_Education_InstitutionCompanyId",
                table: "Education");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Experience");

            migrationBuilder.DropColumn(
                name: "InstitutionCompanyId",
                table: "Education");
        }
    }
}
