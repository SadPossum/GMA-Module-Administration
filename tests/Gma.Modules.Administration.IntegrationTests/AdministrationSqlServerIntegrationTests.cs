namespace Gma.Modules.Administration.IntegrationTests;

using Gma.Modules.Administration.IntegrationTests.Support;
using Gma.Modules.Administration.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

[Collection("Administration relational")]
public sealed class AdministrationSqlServerIntegrationTests : AdministrationRelationalIntegrationTests
{
    protected override async Task<AdministrationTestDatabase> CreateDatabaseAsync(string databaseName)
    {
        _ = databaseName;
        MsSqlContainer sqlServer = new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await sqlServer.StartAsync();
        string connectionString = sqlServer.GetConnectionString();

        return new AdministrationTestDatabase(
            sqlServer,
            () => new DbContextOptionsBuilder<AdminDbContext>()
                .UseSqlServer(connectionString, provider => provider
                    .MigrationsAssembly(AdminMigrations.SqlServerAssembly)
                    .MigrationsHistoryTable(AdminMigrations.HistoryTable, AdminMigrations.Schema))
                .Options,
            lastLegacyRbacMigration: "20260702014633_AddAdminRbacReadPathIndexes",
            identifier => $"[{identifier}]");
    }
}
