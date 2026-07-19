namespace Gma.Modules.Administration.Tests;

using Gma.Framework.Administration;
using Gma.Framework.Results;
using Gma.Framework.Runtime.Time;
using Gma.Modules.Administration.Application;
using Gma.Modules.Administration.Application.Commands;
using Gma.Modules.Administration.Application.Handlers;
using Gma.Modules.Administration.Application.Models;
using Gma.Modules.Administration.Application.Ports;
using Gma.Modules.Administration.Application.Queries;
using Gma.Modules.Administration.Application.Validation;
using Microsoft.Extensions.Options;
using Xunit;

[Trait("Category", "Unit")]
public sealed class AdministrationAuditHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task List_shapes_a_bounded_page_and_continuation_cursor()
    {
        StubRepository repository = new()
        {
            ListResult =
            [
                Entry(3, Now),
                Entry(2, Now.AddMinutes(-1)),
                Entry(1, Now.AddMinutes(-2))
            ]
        };
        AdministrationAuditOptions configured = new()
        {
            DefaultPageSize = 1,
            MaxPageSize = 2
        };
        ListAdministrationAuditEntriesQueryHandler handler = new(
            repository,
            Options.Create(configured));

        Result<AdministrationAuditPage> handled = await handler.HandleAsync(
            new ListAdministrationAuditEntriesQuery(
                " tenant-a ",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                Limit: 99),
            CancellationToken.None);

        Assert.True(handled.IsSuccess, handled.Error.Message);
        Assert.Equal(2, handled.Value.Items.Count);
        Assert.True(handled.Value.HasMore);
        Assert.NotNull(handled.Value.NextCursor);
        Assert.Equal(2, handled.Value.Limit);
        Assert.Equal(3, repository.CapturedTake);
        Assert.Equal("tenant-a", repository.CapturedFilter?.TenantId);
        Assert.True(AdministrationAuditCursorCodec.TryDecode(
            handled.Value.NextCursor,
            out AdministrationAuditCursor? cursor));
        Assert.Equal(handled.Value.Items[^1].Id, cursor?.Id);
    }

    [Fact]
    public async Task List_rejects_a_bad_cursor_without_reading_persistence()
    {
        StubRepository repository = new();
        ListAdministrationAuditEntriesQueryHandler handler = new(
            repository,
            Options.Create(new AdministrationAuditOptions()));

        Result<AdministrationAuditPage> handled = await handler.HandleAsync(
            new ListAdministrationAuditEntriesQuery(
                null, null, null, null, null, null, null, null, "bad!", null),
            CancellationToken.None);

        Assert.True(handled.IsFailure);
        Assert.Equal(AdministrationApplicationErrors.AuditCursorInvalid, handled.Error);
        Assert.Equal(0, repository.ListCalls);
    }

    [Fact]
    public async Task Purge_requires_confirmation_and_clamps_the_batch()
    {
        StubRepository repository = new()
        {
            PurgeResult = new AdministrationAuditRetentionBatch(7, HasMore: true)
        };
        AdministrationAuditOptions configured = new()
        {
            DefaultPurgeBatchSize = 2,
            MaxPurgeBatchSize = 10
        };
        PurgeAdministrationAuditEntriesCommandHandler handler = new(
            repository,
            new FixedClock(Now),
            Options.Create(configured));

        Result<AdministrationAuditRetentionResult> denied = await handler.HandleAsync(
            new PurgeAdministrationAuditEntriesCommand(Now.AddDays(-1), 999, Confirmed: false),
            CancellationToken.None);
        Result<AdministrationAuditRetentionResult> purged = await handler.HandleAsync(
            new PurgeAdministrationAuditEntriesCommand(Now.AddDays(-1), 999, Confirmed: true),
            CancellationToken.None);

        Assert.Equal(AdminErrors.ConfirmationRequired, denied.Error);
        Assert.True(purged.IsSuccess, purged.Error.Message);
        Assert.Equal(10, repository.CapturedBatchSize);
        Assert.Equal(7, purged.Value.DeletedCount);
        Assert.True(purged.Value.HasMore);
        Assert.Equal(1, repository.PurgeCalls);
    }

    [Fact]
    public async Task Purge_rejects_a_non_past_cutoff_without_reading_persistence()
    {
        StubRepository repository = new();
        PurgeAdministrationAuditEntriesCommandHandler handler = new(
            repository,
            new FixedClock(Now),
            Options.Create(new AdministrationAuditOptions()));

        Result<AdministrationAuditRetentionResult> handled = await handler.HandleAsync(
            new PurgeAdministrationAuditEntriesCommand(Now, null, Confirmed: true),
            CancellationToken.None);

        Assert.Equal(AdministrationApplicationErrors.AuditPurgeCutoffInvalid, handled.Error);
        Assert.Equal(0, repository.PurgeCalls);
    }

    [Fact]
    public async Task Purge_rejects_a_missing_cutoff_without_reading_persistence()
    {
        StubRepository repository = new();
        PurgeAdministrationAuditEntriesCommandHandler handler = new(
            repository,
            new FixedClock(Now),
            Options.Create(new AdministrationAuditOptions()));

        Result<AdministrationAuditRetentionResult> handled = await handler.HandleAsync(
            new PurgeAdministrationAuditEntriesCommand(null, null, Confirmed: true),
            CancellationToken.None);

        Assert.Equal(AdministrationApplicationErrors.AuditPurgeCutoffInvalid, handled.Error);
        Assert.Equal(0, repository.PurgeCalls);
    }

    [Fact]
    public void Purge_validator_rejects_only_non_positive_supplied_batch_sizes()
    {
        PurgeAdministrationAuditEntriesCommandValidator validator = new();

        Assert.NotEmpty(validator.Validate(
            new PurgeAdministrationAuditEntriesCommand(Now.AddDays(-1), 0, Confirmed: true)));
        Assert.Empty(validator.Validate(
            new PurgeAdministrationAuditEntriesCommand(null, null, Confirmed: false)));
    }

    private static AdministrationAuditEntryDetails Entry(int suffix, DateTimeOffset createdAtUtc) =>
        new(
            Guid.Parse($"00000000-0000-0000-0000-{suffix.ToString("D12", System.Globalization.CultureInfo.InvariantCulture)}"),
            "actor-a",
            "tenant-a",
            "auth.members.list",
            "auth.members.read",
            AdminAuditResult.Succeeded,
            null,
            createdAtUtc);

    private sealed class StubRepository : IAdministrationAuditRepository
    {
        public IReadOnlyList<AdministrationAuditEntryDetails> ListResult { get; init; } = [];
        public AdministrationAuditRetentionBatch PurgeResult { get; init; } = new(0, false);
        public int ListCalls { get; private set; }
        public int PurgeCalls { get; private set; }
        public int CapturedTake { get; private set; }
        public int CapturedBatchSize { get; private set; }
        public AdministrationAuditFilter? CapturedFilter { get; private set; }

        public Task<IReadOnlyList<AdministrationAuditEntryDetails>> ListAsync(
            AdministrationAuditFilter filter,
            AdministrationAuditCursor? cursor,
            int take,
            CancellationToken cancellationToken)
        {
            this.ListCalls++;
            this.CapturedTake = take;
            this.CapturedFilter = filter;
            return Task.FromResult(this.ListResult);
        }

        public Task<AdministrationAuditRetentionBatch> PurgeBeforeAsync(
            DateTimeOffset cutoffUtc,
            int batchSize,
            CancellationToken cancellationToken)
        {
            this.PurgeCalls++;
            this.CapturedBatchSize = batchSize;
            return Task.FromResult(this.PurgeResult);
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
