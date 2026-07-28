using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gma.Modules.Administration.Persistence.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminResourceScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResourceScopeHash",
                schema: "admin",
                table: "audit_entries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceScope",
                schema: "admin",
                table: "audit_entries",
                type: "character varying(888)",
                maxLength: 888,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_TenantId_ResourceScopeHash_CreatedAtUtc_Id",
                schema: "admin",
                table: "audit_entries",
                columns: new[] { "TenantId", "ResourceScopeHash", "CreatedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_entries_TenantId_ResourceScopeHash_CreatedAtUtc_Id",
                schema: "admin",
                table: "audit_entries");

            migrationBuilder.DropColumn(
                name: "ResourceScope",
                schema: "admin",
                table: "audit_entries");

            migrationBuilder.DropColumn(
                name: "ResourceScopeHash",
                schema: "admin",
                table: "audit_entries");
        }
    }
}
