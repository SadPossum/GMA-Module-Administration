namespace Gma.Modules.Administration.Contracts;

using Gma.Framework.Modules;
using Gma.Framework.Permissions;

public static class AdministrationModuleMetadata
{
    public const string Name = "administration";
    public const string Schema = "admin";

    public static ModuleDescriptor Descriptor { get; } = ModuleDescriptor
        .Create(Name)
        .WithSchema(Schema)
        .WithPermissions([
            new ModulePermissionDescriptor(
                AdministrationPermissionCodes.AuditRead,
                "Read administrative audit records.",
                scopeRequirement: PermissionScopeRequirement.Global),
            new ModulePermissionDescriptor(
                AdministrationPermissionCodes.AuditPurge,
                "Purge administrative audit records using an explicit cutoff.",
                scopeRequirement: PermissionScopeRequirement.Global)
        ])
        .Build();
}
