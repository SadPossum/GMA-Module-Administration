namespace Gma.Modules.Administration.Application.Models;

using Gma.Framework.Administration;

public sealed record AdministrationAuditEntryDetails(
    Guid Id,
    string ActorId,
    string? TenantId,
    string Operation,
    string Permission,
    AdminAuditResult Result,
    string? ErrorCode,
    DateTimeOffset CreatedAtUtc);
