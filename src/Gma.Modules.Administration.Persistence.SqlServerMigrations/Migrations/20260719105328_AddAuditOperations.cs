using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gma.Modules.Administration.Persistence.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_entries_ActorId_CreatedAtUtc",
                schema: "admin",
                table: "audit_entries");

            migrationBuilder.DropIndex(
                name: "IX_audit_entries_TenantId_CreatedAtUtc",
                schema: "admin",
                table: "audit_entries");

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_ActorId_CreatedAtUtc_Id",
                schema: "admin",
                table: "audit_entries",
                columns: new[] { "ActorId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_CreatedAtUtc_Id",
                schema: "admin",
                table: "audit_entries",
                columns: new[] { "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_Operation_CreatedAtUtc_Id",
                schema: "admin",
                table: "audit_entries",
                columns: new[] { "Operation", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_Permission_CreatedAtUtc_Id",
                schema: "admin",
                table: "audit_entries",
                columns: new[] { "Permission", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_TenantId_CreatedAtUtc_Id",
                schema: "admin",
                table: "audit_entries",
                columns: new[] { "TenantId", "CreatedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_entries_ActorId_CreatedAtUtc_Id",
                schema: "admin",
                table: "audit_entries");

            migrationBuilder.DropIndex(
                name: "IX_audit_entries_CreatedAtUtc_Id",
                schema: "admin",
                table: "audit_entries");

            migrationBuilder.DropIndex(
                name: "IX_audit_entries_Operation_CreatedAtUtc_Id",
                schema: "admin",
                table: "audit_entries");

            migrationBuilder.DropIndex(
                name: "IX_audit_entries_Permission_CreatedAtUtc_Id",
                schema: "admin",
                table: "audit_entries");

            migrationBuilder.DropIndex(
                name: "IX_audit_entries_TenantId_CreatedAtUtc_Id",
                schema: "admin",
                table: "audit_entries");

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_ActorId_CreatedAtUtc",
                schema: "admin",
                table: "audit_entries",
                columns: new[] { "ActorId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_TenantId_CreatedAtUtc",
                schema: "admin",
                table: "audit_entries",
                columns: new[] { "TenantId", "CreatedAtUtc" });
        }
    }
}
