namespace Gma.Modules.Administration.Application;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Gma.Framework.Administration;

public static class DependencyInjection
{
    public static IServiceCollection AddAdministrationApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddGmaAdministration();

        return services;
    }
}
