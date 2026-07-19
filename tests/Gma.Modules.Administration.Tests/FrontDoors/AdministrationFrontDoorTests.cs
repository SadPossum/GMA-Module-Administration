namespace Gma.Modules.Administration.Tests;

using Gma.Framework.Administration.Cli;
using Gma.Framework.Administration.Api;
using Gma.Framework.Cqrs;
using Gma.Framework.Results;
using Gma.Modules.Administration.AdminApi;
using Gma.Modules.Administration.AdminCli;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Parsing;
using Xunit;

[Trait("Category", "Unit")]
public sealed class AdministrationFrontDoorTests
{
    [Fact]
    public async Task Admin_api_maps_bounded_read_and_confirmed_purge_routes()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddGmaAdministrationApi(builder.Configuration);
        builder.Services.AddScoped<IRequestDispatcher, StubDispatcher>();
        await using WebApplication application = builder.Build();
        AdministrationAdminApiModule module = new();

        module.MapEndpoints(application);

        string[] routes = ((IEndpointRouteBuilder)application).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Contains("/api/admin/audit/", routes);
        Assert.Contains("/api/admin/audit/purge", routes);
    }

    [Fact]
    public void Admin_cli_maps_unambiguous_list_and_purge_commands()
    {
        ServiceCollection services = new();
        services.AddSingleton<AdminCliGlobalOptions>();
        using ServiceProvider provider = services.BuildServiceProvider();
        AdminCliGlobalOptions options = provider.GetRequiredService<AdminCliGlobalOptions>();
        RootCommand root = new("admin");
        root.Options.Add(options.ActorOption);
        root.Options.Add(options.TenantOption);
        root.Options.Add(options.OutputOption);
        AdminCliCommandRegistry registry = new(root, provider);

        new AdministrationAdminCliModule().MapCommands(registry);

        ParseResult list = root.Parse([
            "administration",
            "audit",
            "list",
            "--actor",
            "operator-a",
            "--record-actor",
            "recorded-a"
        ]);
        ParseResult purge = root.Parse([
            "administration",
            "audit",
            "purge",
            "--before",
            "2026-07-01T00:00:00Z",
            "--yes"
        ]);

        Assert.Empty(list.Errors);
        Assert.Empty(purge.Errors);
    }

    private sealed class StubDispatcher : IRequestDispatcher
    {
        public Task<Result<TResponse>> SendAsync<TResponse>(
            ICommand<TResponse> command,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<TResponse>> QueryAsync<TResponse>(
            IQuery<TResponse> query,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
