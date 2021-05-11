using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Anidow.Migrations
{
    public partial class AddCoverData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CoverDataId",
                table: "Episodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoverDataId",
                table: "Anime",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Covers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    File = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Covers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_CoverDataId",
                table: "Episodes",
                column: "CoverDataId");

            migrationBuilder.CreateIndex(
                name: "IX_Anime_CoverDataId",
                table: "Anime",
                column: "CoverDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Anime_Covers_CoverDataId",
                table: "Anime",
                column: "CoverDataId",
                principalTable: "Covers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Episodes_Covers_CoverDataId",
                table: "Episodes",
                column: "CoverDataId",
                principalTable: "Covers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Anime_Covers_CoverDataId",
                table: "Anime");

            migrationBuilder.DropForeignKey(
                name: "FK_Episodes_Covers_CoverDataId",
                table: "Episodes");

            migrationBuilder.DropTable(
                name: "Covers");

            migrationBuilder.DropIndex(
                name: "IX_Episodes_CoverDataId",
                table: "Episodes");

            migrationBuilder.DropIndex(
                name: "IX_Anime_CoverDataId",
                table: "Anime");

            migrationBuilder.DropColumn(
                name: "CoverDataId",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "CoverDataId",
                table: "Anime");
        }
    }
}
