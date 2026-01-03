using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KollectorScum.Api.Migrations
{
    /// <inheritdoc />
    public partial class SetAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set andy.shutt@googlemail.com as admin if the user exists
            migrationBuilder.Sql(@"
                UPDATE ""ApplicationUsers""
                SET ""IsAdmin"" = true
                WHERE LOWER(""Email"") = 'andy.shutt@googlemail.com';
            ");

            // Create invitation for andy.shutt@googlemail.com if no user exists yet
            // This uses a dummy UUID that will be replaced when the user first signs in
            migrationBuilder.Sql(@"
                INSERT INTO ""UserInvitations"" (""Email"", ""CreatedAt"", ""CreatedByUserId"", ""IsUsed"")
                SELECT 'andy.shutt@googlemail.com', NOW(), '00000000-0000-0000-0000-000000000000', false
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""ApplicationUsers"" WHERE LOWER(""Email"") = 'andy.shutt@googlemail.com'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM ""UserInvitations"" WHERE LOWER(""Email"") = 'andy.shutt@googlemail.com'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert admin status
            migrationBuilder.Sql(@"
                UPDATE ""ApplicationUsers""
                SET ""IsAdmin"" = false
                WHERE LOWER(""Email"") = 'andy.shutt@googlemail.com';
            ");

            // Remove the invitation if it wasn't used
            migrationBuilder.Sql(@"
                DELETE FROM ""UserInvitations""
                WHERE LOWER(""Email"") = 'andy.shutt@googlemail.com'
                AND ""IsUsed"" = false
                AND ""CreatedByUserId"" = '00000000-0000-0000-0000-000000000000';
            ");
        }
    }
}
