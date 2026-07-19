namespace Gma.Modules.Administration.Application;

using Gma.Framework.Results;

public static class AdministrationApplicationErrors
{
    public static readonly Error AuditActorInvalid = new(
        "Administration.AuditActorInvalid",
        "The audit actor filter is invalid.");
    public static readonly Error AuditTenantInvalid = new(
        "Administration.AuditTenantInvalid",
        "The audit tenant filter is invalid.");
    public static readonly Error AuditOperationInvalid = new(
        "Administration.AuditOperationInvalid",
        "The audit operation filter is invalid.");
    public static readonly Error AuditPermissionInvalid = new(
        "Administration.AuditPermissionInvalid",
        "The audit permission filter is invalid.");
    public static readonly Error AuditResultInvalid = new(
        "Administration.AuditResultInvalid",
        "The audit result filter is invalid.");
    public static readonly Error AuditErrorCodeInvalid = new(
        "Administration.AuditErrorCodeInvalid",
        "The audit error-code filter is invalid.");
    public static readonly Error AuditTimeRangeInvalid = new(
        "Administration.AuditTimeRangeInvalid",
        "The audit UTC time range is invalid.");
    public static readonly Error AuditCursorInvalid = new(
        "Administration.AuditCursorInvalid",
        "The audit cursor is invalid.");
    public static readonly Error AuditPurgeCutoffInvalid = new(
        "Administration.AuditPurgeCutoffInvalid",
        "The audit purge cutoff is required and must be earlier than the current UTC time.");
}
