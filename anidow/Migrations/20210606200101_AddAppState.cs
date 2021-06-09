using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Anidow.Migrations
{
    public partial class AddAppState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValue: DateTime.UtcNow),
                    FirstStart = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppStates", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppStates");
        }
    }
}
