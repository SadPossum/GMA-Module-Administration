using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gma.Modules.Administration.Persistence.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalAdminResourceScopeReadIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_ResourceScopeHash_CreatedAtUtc_Id",
                schema: "admin",
                table: "audit_entries",
                columns: new[] { "ResourceScopeHash", "CreatedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_entries_ResourceScopeHash_CreatedAtUtc_Id",
                schema: "admin",
                table: "audit_entries");
        }
    }
}
