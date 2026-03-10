using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KollectorScum.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMagicLinkTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApplicationUsers_GoogleSub",
                table: "ApplicationUsers");

            migrationBuilder.AlterColumn<string>(
                name: "GoogleSub",
                table: "ApplicationUsers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateTable(
                name: "MagicLinkTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MagicLinkTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_GoogleSub",
                table: "ApplicationUsers",
                column: "GoogleSub",
                unique: true,
                filter: "\"GoogleSub\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MagicLinkTokens_Email",
                table: "MagicLinkTokens",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_MagicLinkTokens_ExpiresAt",
                table: "MagicLinkTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_MagicLinkTokens_Token",
                table: "MagicLinkTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MagicLinkTokens");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUsers_GoogleSub",
                table: "ApplicationUsers");

            migrationBuilder.AlterColumn<string>(
                name: "GoogleSub",
                table: "ApplicationUsers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_GoogleSub",
                table: "ApplicationUsers",
                column: "GoogleSub",
                unique: true);
        }
    }
}
