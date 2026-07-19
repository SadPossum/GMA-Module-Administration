namespace Gma.Modules.Administration.AdminApi;

using Gma.Framework.Administration;
using Gma.Framework.Administration.Api;
using Gma.Framework.Api.Observability;
using Gma.Framework.Api.Results;
using Gma.Framework.Cqrs;
using Gma.Framework.Results;
using Gma.Modules.Administration.Admin.Contracts;
using Gma.Modules.Administration.Application;
using Gma.Modules.Administration.Application.Commands;
using Gma.Modules.Administration.Application.Models;
using Gma.Modules.Administration.Application.Queries;
using Gma.Modules.Administration.Contracts;
using Gma.Modules.Administration.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

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
        RouteGroupBuilder audit = endpoints.MapGroup("/api/admin/audit")
            .WithModuleName(this.Name)
            .WithTags("Administration Audit")
            .RequireAuthorization();

        audit.MapGet("/", async (
            string? tenant,
            string? actor,
            string? operation,
            string? permission,
            string? result,
            string? errorCode,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            string? cursor,
            int? limit,
            HttpContext context,
            AdminApiExecutor executor,
            IRequestDispatcher dispatcher,
            CancellationToken cancellationToken) =>
            await executor.ExecuteAsync(
                context,
                AdminOperation.Create(
                    AdministrationAdminOperationNames.AuditList,
                    AdministrationAdminPermissions.AuditRead),
                requireTenant: false,
                token =>
                {
                    if (!TryParseAuditResult(result, out AdminAuditResult? parsedResult))
                    {
                        return Task.FromResult(Result.Failure<AdministrationAuditPage>(
                            AdministrationApplicationErrors.AuditResultInvalid));
                    }

                    return dispatcher.QueryAsync(
                        new ListAdministrationAuditEntriesQuery(
                            tenant,
                            actor,
                            operation,
                            permission,
                            parsedResult,
                            errorCode,
                            fromUtc,
                            toUtc,
                            cursor,
                            limit),
                        token);
                },
                cancellationToken,
                errorStatusCodes: ErrorStatusCodes).ConfigureAwait(false))
            .Produces<AdministrationAuditPage>(StatusCodes.Status200OK);

        audit.MapPost("/purge", async (
            PurgeAdministrationAuditRequest request,
            HttpContext context,
            AdminApiExecutor executor,
            IRequestDispatcher dispatcher,
            CancellationToken cancellationToken) =>
            await executor.ExecuteAsync(
                context,
                AdminOperation.Create(
                    AdministrationAdminOperationNames.AuditPurge,
                    AdministrationAdminPermissions.AuditPurge),
                requireTenant: false,
                token => dispatcher.SendAsync(
                    new PurgeAdministrationAuditEntriesCommand(
                        request.BeforeUtc,
                        request.BatchSize,
                        request.Confirmed),
                    token),
                cancellationToken,
                errorStatusCodes: ErrorStatusCodes).ConfigureAwait(false))
            .Produces<AdministrationAuditRetentionResult>(StatusCodes.Status200OK);
    }

    public sealed record PurgeAdministrationAuditRequest(
        DateTimeOffset? BeforeUtc,
        int? BatchSize,
        bool Confirmed);

    private static bool TryParseAuditResult(string? value, out AdminAuditResult? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!AdminAuditResults.TryParse(value, out AdminAuditResult parsed))
        {
            return false;
        }

        result = parsed;
        return true;
    }

    private static readonly ApiErrorStatusCodeMap ErrorStatusCodes = ApiErrorStatusCodeMap.Create(
        new(AdministrationApplicationErrors.AuditActorInvalid.Code, StatusCodes.Status400BadRequest),
        new(AdministrationApplicationErrors.AuditTenantInvalid.Code, StatusCodes.Status400BadRequest),
        new(AdministrationApplicationErrors.AuditOperationInvalid.Code, StatusCodes.Status400BadRequest),
        new(AdministrationApplicationErrors.AuditPermissionInvalid.Code, StatusCodes.Status400BadRequest),
        new(AdministrationApplicationErrors.AuditResultInvalid.Code, StatusCodes.Status400BadRequest),
        new(AdministrationApplicationErrors.AuditErrorCodeInvalid.Code, StatusCodes.Status400BadRequest),
        new(AdministrationApplicationErrors.AuditTimeRangeInvalid.Code, StatusCodes.Status400BadRequest),
        new(AdministrationApplicationErrors.AuditCursorInvalid.Code, StatusCodes.Status400BadRequest),
        new(AdministrationApplicationErrors.AuditPurgeCutoffInvalid.Code, StatusCodes.Status400BadRequest),
        new(AdminErrors.ConfirmationRequired.Code, StatusCodes.Status400BadRequest));
}
