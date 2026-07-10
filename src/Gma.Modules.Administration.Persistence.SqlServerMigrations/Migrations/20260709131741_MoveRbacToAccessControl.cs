using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gma.Modules.Administration.Persistence.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class MoveRbacToAccessControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Administration no longer maps RBAC tables. Leave legacy tables in place because
            // AccessControl migrations are module-owned and may be applied independently.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: legacy RBAC tables were intentionally preserved by Up.
        }
    }
}
