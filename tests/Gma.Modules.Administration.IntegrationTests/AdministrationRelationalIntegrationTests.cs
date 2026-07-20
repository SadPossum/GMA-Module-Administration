namespace Gma.Modules.Administration.IntegrationTests;

using System.Data.Common;
using System.Globalization;
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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Xunit;

[Trait("Category", "Docker")]
[Trait("Category", "Integration")]
public abstract class AdministrationRelationalIntegrationTests
{
    private const string LegacyPrincipalId = "legacy-actor";
    private const string LegacyRoleName = "legacy-operator";
    private const string LegacyTenantId = "tenant-a";
    private const string LegacyPermission = "auth.members.read";
    private static readonly Guid LegacyRoleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid LegacyAssignmentId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid LegacyPermissionId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [DockerFact]
    public async Task Audit_sink_persists_normalized_metadata_and_utc_timestamp()
    {
        await using AdministrationTestDatabase database = await this.CreateDatabaseAsync("administration_sink_tests");
        await using AdminDbContext dbContext = await database.CreateMigratedDbContextAsync();
        AdminAuditSink sink = new(dbContext);
        DateTimeOffset recordedAt = new(2026, 7, 19, 15, 0, 0, TimeSpan.FromHours(3));

        await sink.RecordAsync(
            new AdminAuditRecord(
                Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                " Actor-A ",
                " tenant-a ",
                " Auth.Members.List ",
                " Auth.Members.Read ",
                AdminAuditResult.Canceled,
                AdminErrors.OperationCanceled.Code,
                recordedAt),
            CancellationToken.None);

        AdminAuditEntry entry = Assert.Single(await dbContext.AuditEntries.AsNoTracking().ToArrayAsync());
        Assert.Equal("Actor-A", entry.ActorId);
        Assert.Equal("tenant-a", entry.TenantId);
        Assert.Equal("auth.members.list", entry.Operation);
        Assert.Equal("auth.members.read", entry.Permission);
        Assert.Equal(AdminAuditResults.Canceled, entry.Result);
        Assert.Equal(AdminErrors.OperationCanceled.Code, entry.ErrorCode);
        Assert.Equal(recordedAt.ToUniversalTime(), entry.CreatedAtUtc);
    }

    [DockerFact]
    public async Task Exact_filters_are_isolated()
    {
        await using AdministrationTestDatabase database = await this.CreateDatabaseAsync("administration_filter_tests");
        await using AdminDbContext dbContext = await database.CreateMigratedDbContextAsync();
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
            AdminAuditResult.Succeeded,
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
    public async Task Cursor_traversal_is_complete_and_duplicate_free()
    {
        await using AdministrationTestDatabase database = await this.CreateDatabaseAsync("administration_cursor_tests");
        await using AdminDbContext dbContext = await database.CreateMigratedDbContextAsync();
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
        await using AdministrationTestDatabase database = await this.CreateDatabaseAsync("administration_retention_tests");
        await using AdminDbContext dbContext = await database.CreateMigratedDbContextAsync();
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

    [DockerFact]
    public async Task Current_migrations_preserve_legacy_rbac_rows()
    {
        await using AdministrationTestDatabase database = await this.CreateDatabaseAsync("administration_legacy_tests");
        await using AdminDbContext dbContext = database.CreateDbContext();
        IMigrator migrator = dbContext.GetService<IMigrator>();
        await migrator.MigrateAsync(database.LastLegacyRbacMigration);
        await SeedLegacyRbacAsync(dbContext, database);

        await migrator.MigrateAsync();
        dbContext.AuditEntries.Add(Entry(99));
        await dbContext.SaveChangesAsync();

        Assert.Equal(LegacyPrincipalId, await ReadLegacyValueAsync(dbContext, database, "principals", "Id", "Id", LegacyPrincipalId));
        Assert.Equal(LegacyRoleName, await ReadLegacyValueAsync(dbContext, database, "roles", "Name", "Id", LegacyRoleId));
        Assert.Equal(LegacyTenantId, await ReadLegacyValueAsync(dbContext, database, "principal_roles", "TenantId", "Id", LegacyAssignmentId));
        Assert.Equal(LegacyPermission, await ReadLegacyValueAsync(dbContext, database, "role_permissions", "PermissionCode", "Id", LegacyPermissionId));
        Assert.Equal(Id(99), Assert.Single(await dbContext.AuditEntries.AsNoTracking().ToArrayAsync()).Id);
    }

    protected abstract Task<AdministrationTestDatabase> CreateDatabaseAsync(string databaseName);

    private static async Task SeedLegacyRbacAsync(
        AdminDbContext dbContext,
        AdministrationTestDatabase database)
    {
        await ExecuteLegacyCommandAsync(
            dbContext,
            $"INSERT INTO {database.Table("principals")} ({database.Column("Id")}, {database.Column("CreatedAtUtc")}) VALUES (@id, @createdAtUtc)",
            ("@id", LegacyPrincipalId),
            ("@createdAtUtc", Now));
        await ExecuteLegacyCommandAsync(
            dbContext,
            $"INSERT INTO {database.Table("roles")} ({database.Column("Id")}, {database.Column("Name")}, {database.Column("CreatedAtUtc")}) VALUES (@id, @name, @createdAtUtc)",
            ("@id", LegacyRoleId),
            ("@name", LegacyRoleName),
            ("@createdAtUtc", Now));
        await ExecuteLegacyCommandAsync(
            dbContext,
            $"INSERT INTO {database.Table("principal_roles")} ({database.Column("Id")}, {database.Column("PrincipalId")}, {database.Column("RoleId")}, {database.Column("TenantId")}, {database.Column("CreatedAtUtc")}) VALUES (@id, @principalId, @roleId, @tenantId, @createdAtUtc)",
            ("@id", LegacyAssignmentId),
            ("@principalId", LegacyPrincipalId),
            ("@roleId", LegacyRoleId),
            ("@tenantId", LegacyTenantId),
            ("@createdAtUtc", Now));
        await ExecuteLegacyCommandAsync(
            dbContext,
            $"INSERT INTO {database.Table("role_permissions")} ({database.Column("Id")}, {database.Column("RoleId")}, {database.Column("PermissionCode")}, {database.Column("CreatedAtUtc")}) VALUES (@id, @roleId, @permission, @createdAtUtc)",
            ("@id", LegacyPermissionId),
            ("@roleId", LegacyRoleId),
            ("@permission", LegacyPermission),
            ("@createdAtUtc", Now));
    }

    private static async Task ExecuteLegacyCommandAsync(
        AdminDbContext dbContext,
        string commandText,
        params (string Name, object Value)[] parameters)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        await dbContext.Database.OpenConnectionAsync();
        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = commandText;
            AddParameters(command, parameters);
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private static async Task<string?> ReadLegacyValueAsync(
        AdminDbContext dbContext,
        AdministrationTestDatabase database,
        string table,
        string selectedColumn,
        string keyColumn,
        object key)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        await dbContext.Database.OpenConnectionAsync();
        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText =
                $"SELECT {database.Column(selectedColumn)} FROM {database.Table(table)} WHERE {database.Column(keyColumn)} = @key";
            AddParameters(command, [("@key", key)]);
            object? value = await command.ExecuteScalarAsync();
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private static void AddParameters(
        DbCommand command,
        IEnumerable<(string Name, object Value)> parameters)
    {
        foreach ((string name, object value) in parameters)
        {
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }
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
        Guid.Parse($"00000000-0000-0000-0000-{suffix.ToString("D12", CultureInfo.InvariantCulture)}");

    private sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
