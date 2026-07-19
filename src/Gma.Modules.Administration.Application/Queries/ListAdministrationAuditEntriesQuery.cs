namespace Gma.Modules.Administration.Application.Queries;

using Gma.Framework.Administration;
using Gma.Framework.Cqrs;
using Gma.Modules.Administration.Application.Models;

public sealed record ListAdministrationAuditEntriesQuery(
    string? TenantId,
    string? ActorId,
    string? Operation,
    string? Permission,
    AdminAuditResult? Result,
    string? ErrorCode,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    string? Cursor,
    int? Limit) : IQuery<AdministrationAuditPage>;
