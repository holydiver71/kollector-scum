using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KollectorScum.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_DateAdded",
                table: "MusicReleases",
                column: "DateAdded");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_OrigReleaseYear",
                table: "MusicReleases",
                column: "OrigReleaseYear");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_UserId_DateAdded",
                table: "MusicReleases",
                columns: new[] { "UserId", "DateAdded" });

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_UserId_Title",
                table: "MusicReleases",
                columns: new[] { "UserId", "Title" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MusicReleases_DateAdded",
                table: "MusicReleases");

            migrationBuilder.DropIndex(
                name: "IX_MusicReleases_OrigReleaseYear",
                table: "MusicReleases");

            migrationBuilder.DropIndex(
                name: "IX_MusicReleases_UserId_DateAdded",
                table: "MusicReleases");

            migrationBuilder.DropIndex(
                name: "IX_MusicReleases_UserId_Title",
                table: "MusicReleases");
        }
    }
}
