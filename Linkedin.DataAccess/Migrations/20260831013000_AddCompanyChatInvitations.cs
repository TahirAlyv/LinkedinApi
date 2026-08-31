using System;
using Linkedin.Core.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Linkedin.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260831013000_AddCompanyChatInvitations")]
    public partial class AddCompanyChatInvitations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvitedByUserId", table: "Chats", type: "nvarchar(450)",
                maxLength: 450, nullable: true);
            migrationBuilder.AddColumn<DateTime>(
                name: "InvitationRespondedAt", table: "Chats", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "InvitationStatus", table: "Chats", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<bool>(
                name: "RequiresAcceptance", table: "Chats", type: "bit", nullable: false, defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "InvitedByUserId", table: "Chats");
            migrationBuilder.DropColumn(name: "InvitationRespondedAt", table: "Chats");
            migrationBuilder.DropColumn(name: "InvitationStatus", table: "Chats");
            migrationBuilder.DropColumn(name: "RequiresAcceptance", table: "Chats");
        }
    }
}
