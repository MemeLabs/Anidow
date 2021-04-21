using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Anidow.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Anime",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    Site = table.Column<int>(nullable: false),
                    Folder = table.Column<string>(nullable: true),
                    Resolution = table.Column<string>(nullable: true),
                    Released = table.Column<DateTime>(nullable: false),
                    Cover = table.Column<string>(nullable: true),
                    GroupId = table.Column<string>(nullable: true),
                    GroupUrl = table.Column<string>(nullable: true),
                    Group = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anime", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Episodes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    Site = table.Column<int>(nullable: false),
                    Folder = table.Column<string>(nullable: true),
                    File = table.Column<string>(nullable: true),
                    TorrentId = table.Column<string>(nullable: true),
                    DownloadLink = table.Column<string>(nullable: true),
                    Released = table.Column<DateTime>(nullable: false),
                    Watched = table.Column<bool>(nullable: false),
                    WatchedDate = table.Column<DateTime>(nullable: false),
                    Hide = table.Column<bool>(nullable: false),
                    Link = table.Column<string>(nullable: true),
                    AnimeId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episodes", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Anime");

            migrationBuilder.DropTable(
                name: "Episodes");
        }
    }
}
