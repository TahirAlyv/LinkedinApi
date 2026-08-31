using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Linkedin.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileCurrentModelAndRepairSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Events', N'ImageUrl') IS NULL
BEGIN
    ALTER TABLE [dbo].[Events]
    ADD [ImageUrl] nvarchar(max) NULL;
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Repair migration:
            // rollback zamanı mövcud event şəkillərini silmirik.
        }
    }
}