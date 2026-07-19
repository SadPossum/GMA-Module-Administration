namespace Gma.Modules.Administration.Application.Models;

public sealed record AdministrationAuditEntryDetails(
    Guid Id,
    string ActorId,
    string? TenantId,
    string Operation,
    string Permission,
    string Result,
    string? ErrorCode,
    DateTimeOffset CreatedAtUtc);
