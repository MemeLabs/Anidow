using Microsoft.EntityFrameworkCore.Migrations;

namespace Anidow.Migrations
{
    public partial class AlternativeTitles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlternativeTitles",
                table: "AniListAnime",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlternativeTitles",
                table: "AniListAnime");
        }
    }
}
