using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KollectorScum.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFacebookAuth : Migration
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

            migrationBuilder.AddColumn<string>(
                name: "FacebookSub",
                table: "ApplicationUsers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_FacebookSub",
                table: "ApplicationUsers",
                column: "FacebookSub",
                unique: true,
                filter: "\"FacebookSub\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_GoogleSub",
                table: "ApplicationUsers",
                column: "GoogleSub",
                unique: true,
                filter: "\"GoogleSub\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApplicationUsers_FacebookSub",
                table: "ApplicationUsers");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUsers_GoogleSub",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "FacebookSub",
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
