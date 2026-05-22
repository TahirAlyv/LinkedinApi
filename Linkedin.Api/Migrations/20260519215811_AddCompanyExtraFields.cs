using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Linkedin.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyExtraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanySize",
                table: "Company",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FoundedYear",
                table: "Company",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanySize",
                table: "Company");

            migrationBuilder.DropColumn(
                name: "FoundedYear",
                table: "Company");
        }
    }
}
