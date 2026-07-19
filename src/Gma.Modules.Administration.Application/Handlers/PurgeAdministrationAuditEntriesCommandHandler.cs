namespace Gma.Modules.Administration.Application.Handlers;

using Gma.Framework.Administration;
using Gma.Framework.Cqrs;
using Gma.Framework.Results;
using Gma.Framework.Runtime.Time;
using Gma.Modules.Administration.Application.Commands;
using Gma.Modules.Administration.Application.Models;
using Gma.Modules.Administration.Application.Ports;
using Microsoft.Extensions.Options;

internal sealed class PurgeAdministrationAuditEntriesCommandHandler(
    IAdministrationAuditRepository repository,
    ISystemClock clock,
    IOptions<AdministrationAuditOptions> options)
    : ICommandHandler<PurgeAdministrationAuditEntriesCommand, AdministrationAuditRetentionResult>
{
    public async Task<Result<AdministrationAuditRetentionResult>> HandleAsync(
        PurgeAdministrationAuditEntriesCommand command,
        CancellationToken cancellationToken)
    {
        if (!command.Confirmed)
        {
            return Result.Failure<AdministrationAuditRetentionResult>(
                AdminErrors.ConfirmationRequired);
        }

        if (!command.CutoffUtc.HasValue)
        {
            return Result.Failure<AdministrationAuditRetentionResult>(
                AdministrationApplicationErrors.AuditPurgeCutoffInvalid);
        }

        DateTimeOffset cutoffUtc = command.CutoffUtc.Value.ToUniversalTime();
        if (cutoffUtc >= clock.UtcNow)
        {
            return Result.Failure<AdministrationAuditRetentionResult>(
                AdministrationApplicationErrors.AuditPurgeCutoffInvalid);
        }

        AdministrationAuditOptions configured = options.Value;
        int batchSize = Math.Clamp(
            command.BatchSize ?? configured.DefaultPurgeBatchSize,
            1,
            configured.MaxPurgeBatchSize);
        AdministrationAuditRetentionBatch batch = await repository.PurgeBeforeAsync(
            cutoffUtc,
            batchSize,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(new AdministrationAuditRetentionResult(
            batch.DeletedCount,
            batch.HasMore,
            cutoffUtc,
            batchSize));
    }
}
