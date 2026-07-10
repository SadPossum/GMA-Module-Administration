namespace Gma.Modules.Administration.AdminCli;

using Gma.Modules.Administration.Application;
using Gma.Modules.Administration.Contracts;
using Gma.Modules.Administration.Persistence;
using Microsoft.Extensions.Hosting;
using Gma.Framework.Administration.Cli;

public sealed class AdministrationAdminCliModule : IAdminCliModule
{
    public string Name => AdministrationModuleMetadata.Name;

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.Services.AddAdministrationApplication(builder.Configuration);
        builder.AddAdministrationPersistence();
    }

    public void MapCommands(IAdminCliCommandRegistry commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
    }
}
