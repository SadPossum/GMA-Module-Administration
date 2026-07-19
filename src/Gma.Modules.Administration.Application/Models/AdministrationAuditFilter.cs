namespace Gma.Modules.Administration.Application.Models;

using Gma.Framework.Administration;
using Gma.Framework.Naming;
using Gma.Framework.Results;
using Gma.Modules.Administration.Contracts;

internal sealed record AdministrationAuditFilter(
    string? TenantId,
    string? ActorId,
    string? Operation,
    string? Permission,
    string? ResultName,
    string? ErrorCode,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc)
{
    private static readonly AdminPermission FilterValidationPermission =
        AdminPermission.Create(AdministrationPermissionCodes.AuditRead);

    public static Result<AdministrationAuditFilter> Create(
        string? tenantId,
        string? actorId,
        string? operation,
        string? permission,
        string? result,
        string? errorCode,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc)
    {
        string? normalizedTenantId = null;
        if (!string.IsNullOrWhiteSpace(tenantId) &&
            !TenantIds.TryNormalize(tenantId, out normalizedTenantId))
        {
            return Result.Failure<AdministrationAuditFilter>(
                AdministrationApplicationErrors.AuditTenantInvalid);
        }

        string? normalizedActorId = null;
        if (!string.IsNullOrWhiteSpace(actorId))
        {
            if (!AdminActor.TrySystem(actorId, out AdminActor? actor))
            {
                return Result.Failure<AdministrationAuditFilter>(
                    AdministrationApplicationErrors.AuditActorInvalid);
            }

            normalizedActorId = actor.Id;
        }

        string? normalizedOperation = null;
        if (!string.IsNullOrWhiteSpace(operation))
        {
            if (!AdminOperation.TryCreate(operation, FilterValidationPermission, out AdminOperation? parsedOperation))
            {
                return Result.Failure<AdministrationAuditFilter>(
                    AdministrationApplicationErrors.AuditOperationInvalid);
            }

            normalizedOperation = parsedOperation.Name;
        }

        string? normalizedPermission = null;
        if (!string.IsNullOrWhiteSpace(permission))
        {
            if (!AdminPermission.TryCreate(permission, out AdminPermission? parsedPermission))
            {
                return Result.Failure<AdministrationAuditFilter>(
                    AdministrationApplicationErrors.AuditPermissionInvalid);
            }

            normalizedPermission = parsedPermission.Code;
        }

        string? normalizedResult = null;
        if (!string.IsNullOrWhiteSpace(result))
        {
            normalizedResult = result.Trim().ToLowerInvariant();
            if (normalizedResult is not (
                AdminAuditResults.Succeeded or
                AdminAuditResults.Denied or
                AdminAuditResults.Failed))
            {
                return Result.Failure<AdministrationAuditFilter>(
                    AdministrationApplicationErrors.AuditResultInvalid);
            }
        }

        string? normalizedErrorCode = null;
        if (!string.IsNullOrWhiteSpace(errorCode) &&
            !Error.TryNormalizeCode(errorCode, out normalizedErrorCode))
        {
            return Result.Failure<AdministrationAuditFilter>(
                AdministrationApplicationErrors.AuditErrorCodeInvalid);
        }

        DateTimeOffset? normalizedFromUtc = fromUtc?.ToUniversalTime();
        DateTimeOffset? normalizedToUtc = toUtc?.ToUniversalTime();
        if (normalizedFromUtc.HasValue &&
            normalizedToUtc.HasValue &&
            normalizedFromUtc.Value >= normalizedToUtc.Value)
        {
            return Result.Failure<AdministrationAuditFilter>(
                AdministrationApplicationErrors.AuditTimeRangeInvalid);
        }

        return Result.Success(new AdministrationAuditFilter(
            normalizedTenantId,
            normalizedActorId,
            normalizedOperation,
            normalizedPermission,
            normalizedResult,
            normalizedErrorCode,
            normalizedFromUtc,
            normalizedToUtc));
    }
}
