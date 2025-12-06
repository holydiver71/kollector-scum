using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KollectorScum.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddKollectionsFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kollections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KollectionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KollectionId = table.Column<int>(type: "integer", nullable: false),
                    MusicReleaseId = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KollectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KollectionItems_Kollections_KollectionId",
                        column: x => x.KollectionId,
                        principalTable: "Kollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KollectionItems_MusicReleases_MusicReleaseId",
                        column: x => x.MusicReleaseId,
                        principalTable: "MusicReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kollections_Name",
                table: "Kollections",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_KollectionItems_KollectionId",
                table: "KollectionItems",
                column: "KollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_KollectionItems_MusicReleaseId",
                table: "KollectionItems",
                column: "MusicReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_KollectionItems_KollectionId_MusicReleaseId",
                table: "KollectionItems",
                columns: new[] { "KollectionId", "MusicReleaseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KollectionItems");

            migrationBuilder.DropTable(
                name: "Kollections");
        }
    }
}
