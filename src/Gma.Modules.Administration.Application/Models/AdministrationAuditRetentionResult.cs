namespace Gma.Modules.Administration.Application.Models;

public sealed record AdministrationAuditRetentionResult(
    int DeletedCount,
    bool HasMore,
    DateTimeOffset CutoffUtc,
    int BatchSize);
