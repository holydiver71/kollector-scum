using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KollectorScum.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kollections_Name",
                table: "Kollections");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Stores",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Packagings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "MusicReleases",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Lists",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Labels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Kollections",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Genres",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Formats",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Countries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Artists",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "ApplicationUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Assign all existing data to the admin user (12337b39-c346-449c-b269-33b2e820d74f)
            var adminUserId = "12337b39-c346-449c-b269-33b2e820d74f";
            
            migrationBuilder.Sql($"UPDATE \"MusicReleases\" SET \"UserId\" = '{adminUserId}' WHERE \"UserId\" = '00000000-0000-0000-0000-000000000000'");
            migrationBuilder.Sql($"UPDATE \"Lists\" SET \"UserId\" = '{adminUserId}' WHERE \"UserId\" = '00000000-0000-0000-0000-000000000000'");
            migrationBuilder.Sql($"UPDATE \"Kollections\" SET \"UserId\" = '{adminUserId}' WHERE \"UserId\" = '00000000-0000-0000-0000-000000000000'");
            migrationBuilder.Sql($"UPDATE \"Artists\" SET \"UserId\" = '{adminUserId}' WHERE \"UserId\" = '00000000-0000-0000-0000-000000000000'");
            migrationBuilder.Sql($"UPDATE \"Genres\" SET \"UserId\" = '{adminUserId}' WHERE \"UserId\" = '00000000-0000-0000-0000-000000000000'");
            migrationBuilder.Sql($"UPDATE \"Labels\" SET \"UserId\" = '{adminUserId}' WHERE \"UserId\" = '00000000-0000-0000-0000-000000000000'");
            migrationBuilder.Sql($"UPDATE \"Countries\" SET \"UserId\" = '{adminUserId}' WHERE \"UserId\" = '00000000-0000-0000-0000-000000000000'");
            migrationBuilder.Sql($"UPDATE \"Formats\" SET \"UserId\" = '{adminUserId}' WHERE \"UserId\" = '00000000-0000-0000-0000-000000000000'");
            migrationBuilder.Sql($"UPDATE \"Packagings\" SET \"UserId\" = '{adminUserId}' WHERE \"UserId\" = '00000000-0000-0000-0000-000000000000'");
            migrationBuilder.Sql($"UPDATE \"Stores\" SET \"UserId\" = '{adminUserId}' WHERE \"UserId\" = '00000000-0000-0000-0000-000000000000'");
            
            // Set IsAdmin flag for the admin user
            migrationBuilder.Sql($"UPDATE \"ApplicationUsers\" SET \"IsAdmin\" = true WHERE \"Id\" = '{adminUserId}'");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_UserId",
                table: "Stores",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_UserId_Name",
                table: "Stores",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Packagings_UserId",
                table: "Packagings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Packagings_UserId_Name",
                table: "Packagings",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_UserId",
                table: "MusicReleases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Lists_UserId",
                table: "Lists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Labels_UserId",
                table: "Labels",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Labels_UserId_Name",
                table: "Labels",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kollections_UserId",
                table: "Kollections",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Kollections_UserId_Name",
                table: "Kollections",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Genres_UserId",
                table: "Genres",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Genres_UserId_Name",
                table: "Genres",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Formats_UserId",
                table: "Formats",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Formats_UserId_Name",
                table: "Formats",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_UserId",
                table: "Countries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_UserId_Name",
                table: "Countries",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Artists_UserId",
                table: "Artists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Artists_UserId_Name",
                table: "Artists",
                columns: new[] { "UserId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stores_UserId",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_Stores_UserId_Name",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_Packagings_UserId",
                table: "Packagings");

            migrationBuilder.DropIndex(
                name: "IX_Packagings_UserId_Name",
                table: "Packagings");

            migrationBuilder.DropIndex(
                name: "IX_MusicReleases_UserId",
                table: "MusicReleases");

            migrationBuilder.DropIndex(
                name: "IX_Lists_UserId",
                table: "Lists");

            migrationBuilder.DropIndex(
                name: "IX_Labels_UserId",
                table: "Labels");

            migrationBuilder.DropIndex(
                name: "IX_Labels_UserId_Name",
                table: "Labels");

            migrationBuilder.DropIndex(
                name: "IX_Kollections_UserId",
                table: "Kollections");

            migrationBuilder.DropIndex(
                name: "IX_Kollections_UserId_Name",
                table: "Kollections");

            migrationBuilder.DropIndex(
                name: "IX_Genres_UserId",
                table: "Genres");

            migrationBuilder.DropIndex(
                name: "IX_Genres_UserId_Name",
                table: "Genres");

            migrationBuilder.DropIndex(
                name: "IX_Formats_UserId",
                table: "Formats");

            migrationBuilder.DropIndex(
                name: "IX_Formats_UserId_Name",
                table: "Formats");

            migrationBuilder.DropIndex(
                name: "IX_Countries_UserId",
                table: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_Countries_UserId_Name",
                table: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_Artists_UserId",
                table: "Artists");

            migrationBuilder.DropIndex(
                name: "IX_Artists_UserId_Name",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Packagings");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MusicReleases");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Lists");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Labels");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Kollections");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Genres");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Formats");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "ApplicationUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Kollections_Name",
                table: "Kollections",
                column: "Name",
                unique: true);
        }
    }
}
