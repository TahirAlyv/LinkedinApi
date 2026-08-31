using System;
using Linkedin.Core.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Linkedin.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260830214500_AddPerUserChatDeletion")]
    public partial class AddPerUserChatDeletion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiverHiddenAt",
                table: "Chats",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SenderHiddenAt",
                table: "Chats",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiverHiddenAt",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "SenderHiddenAt",
                table: "Chats");
        }
    }
}
