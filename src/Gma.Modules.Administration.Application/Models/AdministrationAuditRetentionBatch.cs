namespace Gma.Modules.Administration.Application.Models;

internal sealed record AdministrationAuditRetentionBatch(int DeletedCount, bool HasMore);
