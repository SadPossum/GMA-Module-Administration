namespace Gma.Modules.Administration.Tests;

using Gma.Framework.Permissions;
using Gma.Modules.Administration.Contracts;
using Xunit;

[Trait("Category", "Unit")]
public sealed class AdministrationModuleMetadataTests
{
    [Fact]
    public void Descriptor_declares_only_global_audit_permissions()
    {
        ModulePermissionDescriptor[] permissions = AdministrationModuleMetadata.Descriptor
            .GetPermissions()
            .OrderBy(permission => permission.Code, StringComparer.Ordinal)
            .ToArray();

        Assert.Collection(
            permissions,
            permission =>
            {
                Assert.Equal(AdministrationPermissionCodes.AuditPurge, permission.Code);
                Assert.Equal(PermissionScopeRequirement.Global, permission.ScopeRequirement);
            },
            permission =>
            {
                Assert.Equal(AdministrationPermissionCodes.AuditRead, permission.Code);
                Assert.Equal(PermissionScopeRequirement.Global, permission.ScopeRequirement);
            });
    }
}
