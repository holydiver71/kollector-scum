using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KollectorScum.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMusicReleaseEntityWithRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MusicReleases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ReleaseYear = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrigReleaseYear = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Artists = table.Column<string>(type: "text", nullable: true),
                    Genres = table.Column<string>(type: "text", nullable: true),
                    Live = table.Column<bool>(type: "boolean", nullable: false),
                    LabelId = table.Column<int>(type: "integer", nullable: true),
                    CountryId = table.Column<int>(type: "integer", nullable: true),
                    LabelNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LengthInSeconds = table.Column<int>(type: "integer", nullable: true),
                    FormatId = table.Column<int>(type: "integer", nullable: true),
                    PurchaseInfo = table.Column<string>(type: "text", nullable: true),
                    PackagingId = table.Column<int>(type: "integer", nullable: true),
                    Images = table.Column<string>(type: "text", nullable: true),
                    Links = table.Column<string>(type: "text", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Media = table.Column<string>(type: "text", nullable: true),
                    ArtistId = table.Column<int>(type: "integer", nullable: true),
                    GenreId = table.Column<int>(type: "integer", nullable: true),
                    StoreId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicReleases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicReleases_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artists",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MusicReleases_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MusicReleases_Formats_FormatId",
                        column: x => x.FormatId,
                        principalTable: "Formats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MusicReleases_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MusicReleases_Labels_LabelId",
                        column: x => x.LabelId,
                        principalTable: "Labels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MusicReleases_Packagings_PackagingId",
                        column: x => x.PackagingId,
                        principalTable: "Packagings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MusicReleases_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_ArtistId",
                table: "MusicReleases",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_CountryId",
                table: "MusicReleases",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_FormatId",
                table: "MusicReleases",
                column: "FormatId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_GenreId",
                table: "MusicReleases",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_LabelId",
                table: "MusicReleases",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_PackagingId",
                table: "MusicReleases",
                column: "PackagingId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_ReleaseYear",
                table: "MusicReleases",
                column: "ReleaseYear");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_StoreId",
                table: "MusicReleases",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_Title",
                table: "MusicReleases",
                column: "Title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MusicReleases");
        }
    }
}
