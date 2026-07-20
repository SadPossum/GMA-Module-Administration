namespace Gma.Modules.Administration.IntegrationTests;

using Gma.Modules.Administration.IntegrationTests.Support;
using Gma.Modules.Administration.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

[Collection("Administration relational")]
public sealed class AdministrationPostgreSqlIntegrationTests : AdministrationRelationalIntegrationTests
{
    protected override async Task<AdministrationTestDatabase> CreateDatabaseAsync(string databaseName)
    {
        PostgreSqlContainer postgreSql = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase(databaseName)
            .Build();
        await postgreSql.StartAsync();
        string connectionString = postgreSql.GetConnectionString();

        return new AdministrationTestDatabase(
            postgreSql,
            () => new DbContextOptionsBuilder<AdminDbContext>()
                .UseNpgsql(connectionString, provider => provider
                    .MigrationsAssembly(AdminMigrations.PostgreSqlAssembly)
                    .MigrationsHistoryTable(AdminMigrations.HistoryTable, AdminMigrations.Schema))
                .Options,
            lastLegacyRbacMigration: "20260702014650_AddAdminRbacReadPathIndexes",
            identifier => $"\"{identifier}\"");
    }
}
