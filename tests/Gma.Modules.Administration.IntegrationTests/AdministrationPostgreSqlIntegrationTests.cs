namespace Gma.Modules.Administration.IntegrationTests;

using Gma.Framework.Administration;
using Gma.Framework.Results;
using Gma.Framework.Runtime.Time;
using Gma.Modules.Administration.Application;
using Gma.Modules.Administration.Application.Commands;
using Gma.Modules.Administration.Application.Handlers;
using Gma.Modules.Administration.Application.Models;
using Gma.Modules.Administration.Application.Queries;
using Gma.Modules.Administration.IntegrationTests.Support;
using Gma.Modules.Administration.Persistence;
using Gma.Modules.Administration.Persistence.Entities;
using Gma.Modules.Administration.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Xunit;

[Trait("Category", "Docker")]
[Trait("Category", "Integration")]
public sealed class AdministrationPostgreSqlIntegrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [DockerFact]
    public async Task Audit_sink_persists_normalized_metadata()
    {
        await using PostgreSqlContainer postgreSql = CreatePostgreSql("administration_sink_tests");
        await postgreSql.StartAsync();
        await using AdminDbContext dbContext = await CreateMigratedDbContextAsync(
            postgreSql.GetConnectionString());
        AdminAuditSink sink = new(dbContext);

        await sink.RecordAsync(
            new AdminAuditRecord(
                Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                " Actor-A ",
                " tenant-a ",
                " Auth.Members.List ",
                " Auth.Members.Read ",
                " Succeeded ",
                null,
                Now),
            CancellationToken.None);

        AdminAuditEntry entry = Assert.Single(await dbContext.AuditEntries.AsNoTracking().ToArrayAsync());
        Assert.Equal("Actor-A", entry.ActorId);
        Assert.Equal("tenant-a", entry.TenantId);
        Assert.Equal("auth.members.list", entry.Operation);
        Assert.Equal("auth.members.read", entry.Permission);
        Assert.Equal(AdminAuditResults.Succeeded, entry.Result);
    }

    [DockerFact]
    public async Task Exact_filters_are_isolated_on_PostgreSql()
    {
        await using PostgreSqlContainer postgreSql = CreatePostgreSql("administration_filter_tests");
        await postgreSql.StartAsync();
        await using AdminDbContext dbContext = await CreateMigratedDbContextAsync(
            postgreSql.GetConnectionString());
        dbContext.AuditEntries.AddRange(
            Entry(1, "actor-a", "tenant-a", "auth.members.list", "auth.members.read", "succeeded", null, Now),
            Entry(2, "actor-b", "tenant-a", "auth.members.list", "auth.members.read", "denied", "Auth.Unauthorized", Now.AddMinutes(-1)),
            Entry(3, "actor-a", "tenant-b", "tasks.runs.list", "tasks.runs.read", "succeeded", null, Now.AddMinutes(-2)));
        await dbContext.SaveChangesAsync();
        AdministrationAuditRepository repository = new(dbContext);
        Result<AdministrationAuditFilter> filter = AdministrationAuditFilter.Create(
            "tenant-a",
            "actor-a",
            "auth.members.list",
            "auth.members.read",
            "succeeded",
            null,
            Now.AddHours(-1),
            Now.AddMinutes(1));

        IReadOnlyList<AdministrationAuditEntryDetails> entries = await repository.ListAsync(
            filter.Value,
            cursor: null,
            take: 10,
            CancellationToken.None);

        AdministrationAuditEntryDetails entry = Assert.Single(entries);
        Assert.Equal(Id(1), entry.Id);
    }

    [DockerFact]
    public async Task Cursor_traversal_is_complete_and_duplicate_free_on_PostgreSql()
    {
        await using PostgreSqlContainer postgreSql = CreatePostgreSql("administration_cursor_tests");
        await postgreSql.StartAsync();
        await using AdminDbContext dbContext = await CreateMigratedDbContextAsync(
            postgreSql.GetConnectionString());
        dbContext.AuditEntries.AddRange(
            Entry(1, createdAtUtc: Now),
            Entry(2, createdAtUtc: Now),
            Entry(3, createdAtUtc: Now.AddMinutes(-1)),
            Entry(4, createdAtUtc: Now.AddMinutes(-2)),
            Entry(5, createdAtUtc: Now.AddMinutes(-3)));
        await dbContext.SaveChangesAsync();
        ListAdministrationAuditEntriesQueryHandler handler = new(
            new AdministrationAuditRepository(dbContext),
            Options.Create(new AdministrationAuditOptions
            {
                DefaultPageSize = 2,
                MaxPageSize = 2
            }));
        List<Guid> visited = [];
        string? cursor = null;

        do
        {
            Result<AdministrationAuditPage> page = await handler.HandleAsync(
                new ListAdministrationAuditEntriesQuery(
                    null, null, null, null, null, null, null, null, cursor, Limit: 2),
                CancellationToken.None);
            Assert.True(page.IsSuccess, page.Error.Message);
            visited.AddRange(page.Value.Items.Select(item => item.Id));
            cursor = page.Value.NextCursor;
        }
        while (cursor is not null);

        Assert.Equal(5, visited.Count);
        Assert.Equal(5, visited.Distinct().Count());
        Assert.Equal(
            await dbContext.AuditEntries.OrderByDescending(entry => entry.CreatedAtUtc)
                .ThenByDescending(entry => entry.Id)
                .Select(entry => entry.Id)
                .ToArrayAsync(),
            visited);
    }

    [DockerFact]
    public async Task Retention_deletes_only_one_bounded_batch()
    {
        await using PostgreSqlContainer postgreSql = CreatePostgreSql("administration_retention_tests");
        await postgreSql.StartAsync();
        await using AdminDbContext dbContext = await CreateMigratedDbContextAsync(
            postgreSql.GetConnectionString());
        dbContext.AuditEntries.AddRange(
            Entry(1, createdAtUtc: Now.AddDays(-5)),
            Entry(2, createdAtUtc: Now.AddDays(-4)),
            Entry(3, createdAtUtc: Now.AddDays(-3)),
            Entry(4, createdAtUtc: Now.AddDays(-2)),
            Entry(5, createdAtUtc: Now));
        await dbContext.SaveChangesAsync();
        PurgeAdministrationAuditEntriesCommandHandler handler = new(
            new AdministrationAuditRepository(dbContext),
            new FixedClock(Now.AddHours(1)),
            Options.Create(new AdministrationAuditOptions
            {
                DefaultPurgeBatchSize = 2,
                MaxPurgeBatchSize = 2
            }));

        Result<AdministrationAuditRetentionResult> first = await handler.HandleAsync(
            new PurgeAdministrationAuditEntriesCommand(Now.AddDays(-1), 2, Confirmed: true),
            CancellationToken.None);
        Result<AdministrationAuditRetentionResult> second = await handler.HandleAsync(
            new PurgeAdministrationAuditEntriesCommand(Now.AddDays(-1), 2, Confirmed: true),
            CancellationToken.None);

        Assert.Equal(2, first.Value.DeletedCount);
        Assert.True(first.Value.HasMore);
        Assert.Equal(2, second.Value.DeletedCount);
        Assert.False(second.Value.HasMore);
        AdminAuditEntry remaining = Assert.Single(await dbContext.AuditEntries.AsNoTracking().ToArrayAsync());
        Assert.Equal(Id(5), remaining.Id);
    }

    private static AdminAuditEntry Entry(
        int suffix,
        string actorId = "actor-a",
        string? tenantId = "tenant-a",
        string operation = "auth.members.list",
        string permission = "auth.members.read",
        string result = "succeeded",
        string? errorCode = null,
        DateTimeOffset? createdAtUtc = null) =>
        new(
            Id(suffix),
            actorId,
            tenantId,
            operation,
            permission,
            result,
            errorCode,
            createdAtUtc ?? Now);

    private static Guid Id(int suffix) =>
        Guid.Parse($"00000000-0000-0000-0000-{suffix.ToString("D12", System.Globalization.CultureInfo.InvariantCulture)}");

    private static async Task<AdminDbContext> CreateMigratedDbContextAsync(string connectionString)
    {
        DbContextOptions<AdminDbContext> options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseNpgsql(connectionString, provider => provider
                .MigrationsAssembly(AdminMigrations.PostgreSqlAssembly)
                .MigrationsHistoryTable(AdminMigrations.HistoryTable, AdminMigrations.Schema))
            .Options;
        AdminDbContext dbContext = new(options);
        await dbContext.Database.MigrateAsync();
        return dbContext;
    }

    private static PostgreSqlContainer CreatePostgreSql(string database) =>
        new PostgreSqlBuilder("postgres:16-alpine").WithDatabase(database).Build();

    private sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
