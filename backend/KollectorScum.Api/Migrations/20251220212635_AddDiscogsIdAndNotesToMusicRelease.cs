using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KollectorScum.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscogsIdAndNotesToMusicRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscogsId",
                table: "MusicReleases",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "MusicReleases",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_UserId_DiscogsId",
                table: "MusicReleases",
                columns: new[] { "UserId", "DiscogsId" },
                unique: true,
                filter: "\"DiscogsId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MusicReleases_UserId_DiscogsId",
                table: "MusicReleases");

            migrationBuilder.DropColumn(
                name: "DiscogsId",
                table: "MusicReleases");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "MusicReleases");
        }
    }
}
