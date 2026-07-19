namespace Gma.Modules.Administration.Admin.Contracts;

using Gma.Framework.Administration;
using Gma.Modules.Administration.Contracts;

public static class AdministrationAdminPermissions
{
    public static readonly AdminPermission AuditRead = AdminPermission.Create(AdministrationPermissionCodes.AuditRead);
    public static readonly AdminPermission AuditPurge = AdminPermission.Create(AdministrationPermissionCodes.AuditPurge);
}
