namespace Gma.Modules.Administration.AdminApi;

using Gma.Modules.Administration.Application;
using Gma.Modules.Administration.Contracts;
using Gma.Modules.Administration.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Gma.Framework.Administration.Api;

public sealed class AdministrationAdminApiModule : IAdminApiModule
{
    public string Name => AdministrationModuleMetadata.Name;

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.Services.AddAdministrationApplication(builder.Configuration);
        builder.AddAdministrationPersistence();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
    }
}
