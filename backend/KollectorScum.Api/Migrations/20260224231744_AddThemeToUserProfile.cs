using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KollectorScum.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddThemeToUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedTheme",
                table: "UserProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "metal-default");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedTheme",
                table: "UserProfiles");
        }
    }
}
