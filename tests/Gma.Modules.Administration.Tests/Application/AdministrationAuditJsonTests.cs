namespace Gma.Modules.Administration.Tests;

using System.Text.Json;
using Gma.Framework.Administration;
using Gma.Modules.Administration.Application.Models;
using Xunit;

[Trait("Category", "Unit")]
public sealed class AdministrationAuditJsonTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Audit_entry_result_uses_the_framework_wire_name()
    {
        AdministrationAuditEntryDetails entry = new(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            "actor-a",
            "tenant-a",
            "auth.members.list",
            "auth.members.read",
            AdminAuditResult.Succeeded,
            null,
            new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero));

        string json = JsonSerializer.Serialize(entry, JsonOptions);

        Assert.Contains("\"result\":\"succeeded\"", json, StringComparison.Ordinal);
    }
}
