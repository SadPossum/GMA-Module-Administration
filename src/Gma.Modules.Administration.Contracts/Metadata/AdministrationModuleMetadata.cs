namespace Gma.Modules.Administration.Contracts;

using Gma.Framework.Modules;

public static class AdministrationModuleMetadata
{
    public const string Name = "administration";
    public const string Schema = "admin";

    public static ModuleDescriptor Descriptor { get; } = ModuleDescriptor
        .Create(Name)
        .WithSchema(Schema)
        .Build();
}
