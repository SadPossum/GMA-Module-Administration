namespace Gma.Modules.Administration.Application.Models;

internal readonly record struct AdministrationAuditCursor(DateTimeOffset CreatedAtUtc, Guid Id);
