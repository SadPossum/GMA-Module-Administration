namespace Gma.Modules.Administration.Application.Commands;

using Gma.Framework.Cqrs;
using Gma.Modules.Administration.Application.Models;

public sealed record PurgeAdministrationAuditEntriesCommand(
    DateTimeOffset? CutoffUtc,
    int? BatchSize,
    bool Confirmed) : ICommand<AdministrationAuditRetentionResult>;
