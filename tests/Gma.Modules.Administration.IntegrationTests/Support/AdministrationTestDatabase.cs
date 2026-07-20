namespace Gma.Modules.Administration.IntegrationTests.Support;

using Gma.Modules.Administration.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class AdministrationTestDatabase(
    IAsyncDisposable container,
    Func<DbContextOptions<AdminDbContext>> createOptions,
    string lastLegacyRbacMigration,
    Func<string, string> quoteIdentifier) : IAsyncDisposable
{
    public string LastLegacyRbacMigration { get; } = lastLegacyRbacMigration;

    public AdminDbContext CreateDbContext() => new(createOptions());

    public async Task<AdminDbContext> CreateMigratedDbContextAsync()
    {
        AdminDbContext dbContext = this.CreateDbContext();
        try
        {
            await dbContext.Database.MigrateAsync();
            return dbContext;
        }
        catch
        {
            await dbContext.DisposeAsync();
            throw;
        }
    }

    public string Column(string name) => quoteIdentifier(name);

    public string Table(string name) => $"{quoteIdentifier(AdminMigrations.Schema)}.{quoteIdentifier(name)}";

    public ValueTask DisposeAsync() => container.DisposeAsync();
}
