using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Anidow.Migrations
{
    public partial class StatusMini : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowStatusMiniViewAnimeBytesAiring",
                table: "AppStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowStatusMiniViewAnimeBytesAll",
                table: "AppStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowStatusMiniViewNyaa",
                table: "AppStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowStatusMiniViewAnimeBytesAiring",
                table: "AppStates");

            migrationBuilder.DropColumn(
                name: "ShowStatusMiniViewAnimeBytesAll",
                table: "AppStates");

            migrationBuilder.DropColumn(
                name: "ShowStatusMiniViewNyaa",
                table: "AppStates");
        }
    }
}
