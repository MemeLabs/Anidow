using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Anidow.Migrations
{
    public partial class AniListAnimes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AniListAnimeId",
                table: "Anime",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AniListAnime",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    SiteUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Cover = table.Column<string>(type: "TEXT", nullable: true),
                    IdMal = table.Column<int>(type: "INTEGER", nullable: true),
                    AverageScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Episodes = table.Column<int>(type: "INTEGER", nullable: true),
                    Format = table.Column<string>(type: "TEXT", nullable: true),
                    Season = table.Column<string>(type: "TEXT", nullable: true),
                    SeasonYear = table.Column<int>(type: "INTEGER", nullable: true),
                    Genres = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniListAnime", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anime_AniListAnimeId",
                table: "Anime",
                column: "AniListAnimeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Anime_AniListAnime_AniListAnimeId",
                table: "Anime",
                column: "AniListAnimeId",
                principalTable: "AniListAnime",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Anime_AniListAnime_AniListAnimeId",
                table: "Anime");

            migrationBuilder.DropTable(
                name: "AniListAnime");

            migrationBuilder.DropIndex(
                name: "IX_Anime_AniListAnimeId",
                table: "Anime");

            migrationBuilder.DropColumn(
                name: "AniListAnimeId",
                table: "Anime");
        }
    }
}
