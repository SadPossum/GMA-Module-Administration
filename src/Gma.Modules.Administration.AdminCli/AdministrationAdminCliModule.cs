namespace Gma.Modules.Administration.AdminCli;

using System.CommandLine;
using System.Globalization;
using Gma.Framework.Administration;
using Gma.Framework.Administration.Cli;
using Gma.Framework.Cqrs;
using Gma.Framework.Results;
using Gma.Modules.Administration.Admin.Contracts;
using Gma.Modules.Administration.Application;
using Gma.Modules.Administration.Application.Commands;
using Gma.Modules.Administration.Application.Models;
using Gma.Modules.Administration.Application.Queries;
using Gma.Modules.Administration.Contracts;
using Gma.Modules.Administration.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        AdminCliGlobalOptions globalOptions = commands.Services.GetRequiredService<AdminCliGlobalOptions>();
        Command audit = new("audit", "Read and retain administrative audit records.")
        {
            CreateListCommand(commands.Services, globalOptions),
            CreatePurgeCommand(commands.Services, globalOptions)
        };
        Command administration = new(
            AdministrationModuleMetadata.Name,
            "Administration audit operations.")
        {
            audit
        };

        commands.AddCommand(this.Name, administration);
    }

    private static Command CreateListCommand(
        IServiceProvider services,
        AdminCliGlobalOptions globalOptions)
    {
        Option<string?> actorOption = new("--record-actor") { Description = "Exact recorded actor id filter." };
        Option<string?> operationOption = new("--operation") { Description = "Exact operation name filter." };
        Option<string?> permissionOption = new("--permission") { Description = "Exact permission code filter." };
        Option<string?> resultOption = new("--result") { Description = "Result filter: succeeded, denied, or failed." };
        Option<string?> errorCodeOption = new("--error-code") { Description = "Exact error code filter." };
        Option<DateTimeOffset?> fromOption = new("--from") { Description = "Inclusive UTC lower bound." };
        Option<DateTimeOffset?> toOption = new("--to") { Description = "Exclusive UTC upper bound." };
        Option<string?> cursorOption = new("--cursor") { Description = "Opaque continuation cursor." };
        Option<int?> limitOption = new("--limit") { Description = "Maximum records to return." };
        Command command = new("list", "List administrative audit records newest first.")
        {
            actorOption,
            operationOption,
            permissionOption,
            resultOption,
            errorCodeOption,
            fromOption,
            toOption,
            cursorOption,
            limitOption
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            AdminCliExecutor executor = services.GetRequiredService<AdminCliExecutor>();
            string? tenantId = parseResult.GetValue(globalOptions.TenantOption);
            return executor.ExecuteAsync(
                parseResult,
                AdminOperation.Create(
                    AdministrationAdminOperationNames.AuditList,
                    AdministrationAdminPermissions.AuditRead),
                tenantId: null,
                requireTenant: false,
                async (provider, token) =>
                {
                    IRequestDispatcher dispatcher = provider.GetRequiredService<IRequestDispatcher>();
                    Result<AdministrationAuditPage> query = await dispatcher.QueryAsync(
                        new ListAdministrationAuditEntriesQuery(
                            tenantId,
                            parseResult.GetValue(actorOption),
                            parseResult.GetValue(operationOption),
                            parseResult.GetValue(permissionOption),
                            parseResult.GetValue(resultOption),
                            parseResult.GetValue(errorCodeOption),
                            parseResult.GetValue(fromOption),
                            parseResult.GetValue(toOption),
                            parseResult.GetValue(cursorOption),
                            parseResult.GetValue(limitOption)),
                        token).ConfigureAwait(false);
                    if (query.IsSuccess)
                    {
                        WriteAuditPage(
                            query.Value,
                            parseResult.GetValue(globalOptions.OutputOption) ?? AdminCliOutput.Table);
                    }

                    return query;
                },
                cancellationToken);
        });

        return command;
    }

    private static Command CreatePurgeCommand(
        IServiceProvider services,
        AdminCliGlobalOptions globalOptions)
    {
        Option<DateTimeOffset> beforeOption = new("--before")
        {
            Description = "Delete records older than this UTC cutoff.",
            Required = true
        };
        Option<int?> batchSizeOption = new("--batch-size")
        {
            Description = "Maximum records to delete in this call."
        };
        Option<bool> yesOption = new("--yes")
        {
            Description = "Confirm this destructive operation."
        };
        Command command = new("purge", "Delete one bounded batch of old audit records.")
        {
            beforeOption,
            batchSizeOption,
            yesOption
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            AdminCliExecutor executor = services.GetRequiredService<AdminCliExecutor>();
            return executor.ExecuteAsync(
                parseResult,
                AdminOperation.Create(
                    AdministrationAdminOperationNames.AuditPurge,
                    AdministrationAdminPermissions.AuditPurge),
                tenantId: null,
                requireTenant: false,
                async (provider, token) =>
                {
                    IRequestDispatcher dispatcher = provider.GetRequiredService<IRequestDispatcher>();
                    Result<AdministrationAuditRetentionResult> result = await dispatcher.SendAsync(
                        new PurgeAdministrationAuditEntriesCommand(
                            parseResult.GetRequiredValue(beforeOption),
                            parseResult.GetValue(batchSizeOption),
                            parseResult.GetValue(yesOption)),
                        token).ConfigureAwait(false);
                    if (result.IsSuccess)
                    {
                        WriteRetentionResult(
                            result.Value,
                            parseResult.GetValue(globalOptions.OutputOption) ?? AdminCliOutput.Table);
                    }

                    return result;
                },
                cancellationToken);
        });

        return command;
    }

    private static void WriteAuditPage(AdministrationAuditPage page, string output)
    {
        if (string.Equals(
            AdminCliOutput.NormalizeFormat(output),
            AdminCliOutput.Json,
            StringComparison.Ordinal))
        {
            AdminCliOutput.WriteObject(page, output);
            return;
        }

        AdminCliOutput.WriteRows(
            page.Items,
            output,
            [
                ("CreatedAtUtc", item => item.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture)),
                ("Actor", item => item.ActorId),
                ("Tenant", item => item.TenantId ?? string.Empty),
                ("Operation", item => item.Operation),
                ("Result", item => item.Result),
                ("Error", item => item.ErrorCode ?? string.Empty),
                ("Id", item => item.Id.ToString())
            ]);
        if (page.NextCursor is not null)
        {
            AdminCliOutput.WriteMessage($"Next cursor: {page.NextCursor}");
        }
    }

    private static void WriteRetentionResult(
        AdministrationAuditRetentionResult result,
        string output)
    {
        if (string.Equals(
            AdminCliOutput.NormalizeFormat(output),
            AdminCliOutput.Json,
            StringComparison.Ordinal))
        {
            AdminCliOutput.WriteObject(result, output);
            return;
        }

        AdminCliOutput.WriteMessage(
            $"Deleted {result.DeletedCount.ToString(CultureInfo.InvariantCulture)} audit record(s). " +
            $"More eligible: {result.HasMore.ToString(CultureInfo.InvariantCulture)}.");
    }
}
