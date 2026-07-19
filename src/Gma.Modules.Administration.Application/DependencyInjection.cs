namespace Gma.Modules.Administration.Application;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Gma.Framework.Administration;
using Gma.Framework.Application.Composition;

public static class DependencyInjection
{
    public static IServiceCollection AddAdministrationApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddGmaAdministration();
        services.AddApplicationServicesFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddOptions<AdministrationAuditOptions>()
            .Bind(configuration.GetSection(AdministrationAuditOptions.SectionName))
            .Validate(
                AdministrationAuditOptions.IsValid,
                AdministrationAuditOptions.InvalidConfigurationMessage)
            .ValidateOnStart();

        return services;
    }
}
