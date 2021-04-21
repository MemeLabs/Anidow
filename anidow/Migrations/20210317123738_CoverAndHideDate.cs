using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Anidow.Migrations
{
    public partial class CoverAndHideDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cover",
                table: "Episodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HideDate",
                table: "Episodes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cover",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "HideDate",
                table: "Episodes");
        }
    }
}
