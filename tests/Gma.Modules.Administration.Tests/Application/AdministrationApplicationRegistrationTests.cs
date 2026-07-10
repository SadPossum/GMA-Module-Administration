namespace Gma.Modules.Administration.Tests;

using Gma.Modules.Administration.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Gma.Framework.Administration;
using Xunit;

[Trait("Category", "Unit")]
public sealed class AdministrationApplicationRegistrationTests
{
    [Fact]
    public void Administration_application_registration_is_idempotent()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddAdministrationApplication(configuration);
        services.AddAdministrationApplication(configuration);

        Assert.Single(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IAdminAuthorizationService) &&
                descriptor.ImplementationType == typeof(DenyAllAdminAuthorizationService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IAdminAuditSink));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IAdminOperationRunner));
    }

    [Fact]
    public void Administration_application_registration_rejects_null_arguments()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() =>
            DependencyInjection.AddAdministrationApplication(null!, configuration));
        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddAdministrationApplication(null!));
    }
}
