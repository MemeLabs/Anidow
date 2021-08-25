using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Anidow.Migrations
{
    public partial class Notify : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotifyItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Site = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchAll = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotifyItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotifyItemKeywords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Word = table.Column<string>(type: "TEXT", nullable: true),
                    IsRegex = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCaseSensitive = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotifyItemId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotifyItemKeywords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotifyItemKeywords_NotifyItems_NotifyItemId",
                        column: x => x.NotifyItemId,
                        principalTable: "NotifyItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotifyItemMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Site = table.Column<int>(type: "INTEGER", nullable: false),
                    Seen = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    DownloadLink = table.Column<string>(type: "TEXT", nullable: true),
                    Link = table.Column<string>(type: "TEXT", nullable: true),
                    Json = table.Column<string>(type: "TEXT", nullable: true),
                    UserNotified = table.Column<bool>(type: "INTEGER", nullable: false),
                    KeywordsData = table.Column<string>(type: "TEXT", nullable: true),
                    NotifyItemId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotifyItemMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotifyItemMatches_NotifyItems_NotifyItemId",
                        column: x => x.NotifyItemId,
                        principalTable: "NotifyItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotifyItemKeywords_NotifyItemId",
                table: "NotifyItemKeywords",
                column: "NotifyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_NotifyItemMatches_NotifyItemId",
                table: "NotifyItemMatches",
                column: "NotifyItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotifyItemKeywords");

            migrationBuilder.DropTable(
                name: "NotifyItemMatches");

            migrationBuilder.DropTable(
                name: "NotifyItems");
        }
    }
}
