using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KollectorScum.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixUserIsActiveDefaultToTrue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix the DB column default from false to true so new rows created outside of EF
            // (e.g. direct SQL inserts or old binaries) default to active, not deactivated.
            migrationBuilder.Sql("ALTER TABLE \"ApplicationUsers\" ALTER COLUMN \"IsActive\" SET DEFAULT TRUE");

            // Ensure any rows that were created with the wrong default are corrected.
            migrationBuilder.Sql("UPDATE \"ApplicationUsers\" SET \"IsActive\" = TRUE WHERE \"IsActive\" = FALSE");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"ApplicationUsers\" ALTER COLUMN \"IsActive\" SET DEFAULT FALSE");
        }
    }
}
