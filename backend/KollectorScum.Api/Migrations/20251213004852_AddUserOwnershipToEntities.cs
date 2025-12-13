using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KollectorScum.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOwnershipToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kollections_Name",
                table: "Kollections");

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
                table: "Kollections",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_UserId",
                table: "MusicReleases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Lists_UserId",
                table: "Lists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Kollections_UserId_Name",
                table: "Kollections",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Kollections_ApplicationUsers_UserId",
                table: "Kollections",
                column: "UserId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lists_ApplicationUsers_UserId",
                table: "Lists",
                column: "UserId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MusicReleases_ApplicationUsers_UserId",
                table: "MusicReleases",
                column: "UserId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kollections_ApplicationUsers_UserId",
                table: "Kollections");

            migrationBuilder.DropForeignKey(
                name: "FK_Lists_ApplicationUsers_UserId",
                table: "Lists");

            migrationBuilder.DropForeignKey(
                name: "FK_MusicReleases_ApplicationUsers_UserId",
                table: "MusicReleases");

            migrationBuilder.DropIndex(
                name: "IX_MusicReleases_UserId",
                table: "MusicReleases");

            migrationBuilder.DropIndex(
                name: "IX_Lists_UserId",
                table: "Lists");

            migrationBuilder.DropIndex(
                name: "IX_Kollections_UserId_Name",
                table: "Kollections");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MusicReleases");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Lists");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Kollections");

            migrationBuilder.CreateIndex(
                name: "IX_Kollections_Name",
                table: "Kollections",
                column: "Name",
                unique: true);
        }
    }
}
