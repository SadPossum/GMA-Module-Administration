namespace Gma.Modules.Administration.Tests;

using Gma.Framework.Administration;
using Gma.Framework.Results;
using Gma.Modules.Administration.Application;
using Gma.Modules.Administration.Application.Models;
using Xunit;

[Trait("Category", "Unit")]
public sealed class AdministrationAuditFilterTests
{
    [Fact]
    public void Create_normalizes_exact_filters_and_utc_range()
    {
        Result<AdministrationAuditFilter> created = AdministrationAuditFilter.Create(
            " tenant-a ",
            " Actor-A ",
            " Auth.Members.List ",
            " Auth.Members.Read ",
            " SUCCEEDED ",
            " Auth.NotFound ",
            new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.FromHours(2)));

        Assert.True(created.IsSuccess, created.Error.Message);
        Assert.Equal("tenant-a", created.Value.TenantId);
        Assert.Equal("Actor-A", created.Value.ActorId);
        Assert.Equal("auth.members.list", created.Value.Operation);
        Assert.Equal("auth.members.read", created.Value.Permission);
        Assert.Equal(AdminAuditResults.Succeeded, created.Value.ResultName);
        Assert.Equal("Auth.NotFound", created.Value.ErrorCode);
        Assert.Equal(TimeSpan.Zero, created.Value.FromUtc?.Offset);
        Assert.Equal(TimeSpan.Zero, created.Value.ToUtc?.Offset);
    }

    [Theory]
    [InlineData("bad actor", null, null, null, null, "Administration.AuditActorInvalid")]
    [InlineData(null, "bad operation", null, null, null, "Administration.AuditOperationInvalid")]
    [InlineData(null, null, "bad", null, null, "Administration.AuditPermissionInvalid")]
    [InlineData(null, null, null, "unknown", null, "Administration.AuditResultInvalid")]
    [InlineData(null, null, null, null, "bad", "Administration.AuditErrorCodeInvalid")]
    public void Create_rejects_invalid_exact_filters(
        string? actor,
        string? operation,
        string? permission,
        string? result,
        string? errorCode,
        string expectedCode)
    {
        Result<AdministrationAuditFilter> created = AdministrationAuditFilter.Create(
            tenantId: null,
            actor,
            operation,
            permission,
            result,
            errorCode,
            fromUtc: null,
            toUtc: null);

        Assert.True(created.IsFailure);
        Assert.Equal(expectedCode, created.Error.Code);
    }

    [Fact]
    public void Create_rejects_an_empty_or_reversed_time_range()
    {
        DateTimeOffset now = new(2026, 7, 19, 10, 0, 0, TimeSpan.Zero);

        Result<AdministrationAuditFilter> created = AdministrationAuditFilter.Create(
            null, null, null, null, null, null, now, now);

        Assert.True(created.IsFailure);
        Assert.Equal(AdministrationApplicationErrors.AuditTimeRangeInvalid, created.Error);
    }
}
