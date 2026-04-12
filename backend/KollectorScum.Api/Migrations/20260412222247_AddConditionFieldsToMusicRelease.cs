using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KollectorScum.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddConditionFieldsToMusicRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaCondition",
                table: "MusicReleases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SleeveCondition",
                table: "MusicReleases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaCondition",
                table: "MusicReleases");

            migrationBuilder.DropColumn(
                name: "SleeveCondition",
                table: "MusicReleases");
        }
    }
}
