namespace Gma.Modules.Administration.Persistence.Entities;

using Gma.Framework.Administration;

public sealed class AdminAuditEntry
{
    private AdminAuditEntry() { }

    public AdminAuditEntry(AdminAuditRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        this.Id = record.Id;
        this.ActorId = record.ActorId;
        this.TenantId = record.TenantId;
        this.Operation = record.Operation;
        this.Permission = record.Permission;
        this.Result = record.ResultName;
        this.ErrorCode = record.ErrorCode;
        this.CreatedAtUtc = record.CreatedAtUtc;
        this.ResourceScope = record.ResourceScope;
        this.ResourceScopeHash = AdminResourceScopeIndex.Create(record.ResourceScope);
    }

    public AdminAuditEntry(
        Guid id,
        string actorId,
        string? tenantId,
        string operation,
        string permission,
        string result,
        string? errorCode,
        DateTimeOffset createdAtUtc,
        string? resourceScope = null)
        : this(new AdminAuditRecord(
            id,
            actorId,
            tenantId,
            operation,
            permission,
            result,
            errorCode,
            createdAtUtc,
            string.IsNullOrWhiteSpace(resourceScope)
                ? null
                : AdminResourceScope.Parse(resourceScope)))
    {
    }

    public Guid Id { get; private set; }
    public string ActorId { get; private set; } = string.Empty;
    public string? TenantId { get; private set; }
    public string Operation { get; private set; } = string.Empty;
    public string Permission { get; private set; } = string.Empty;
    public string Result { get; private set; } = string.Empty;
    public string? ErrorCode { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string? ResourceScope { get; private set; }
    public string? ResourceScopeHash { get; private set; }
}
