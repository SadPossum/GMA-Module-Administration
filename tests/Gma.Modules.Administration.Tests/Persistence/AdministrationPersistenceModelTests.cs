namespace Gma.Modules.Administration.Tests;

using Gma.Modules.Administration.Persistence.Configurations;
using Gma.Modules.Administration.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

[Trait("Category", "Unit")]
public sealed class AdministrationPersistenceModelTests
{
    [Fact]
    public void Resource_scope_queries_have_global_and_tenant_traversal_indexes()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        new AdminAuditEntryConfiguration().Configure(modelBuilder.Entity<AdminAuditEntry>());
        IMutableEntityType entity = modelBuilder.Model.FindEntityType(typeof(AdminAuditEntry))!;
        string[][] indexes = entity.GetIndexes()
            .Select(index => index.Properties.Select(property => property.Name).ToArray())
            .ToArray();

        Assert.Contains(
            indexes,
            properties => properties.SequenceEqual(
                [
                    nameof(AdminAuditEntry.ResourceScopeHash),
                    nameof(AdminAuditEntry.CreatedAtUtc),
                    nameof(AdminAuditEntry.Id)
                ]));
        Assert.Contains(
            indexes,
            properties => properties.SequenceEqual(
                [
                    nameof(AdminAuditEntry.TenantId),
                    nameof(AdminAuditEntry.ResourceScopeHash),
                    nameof(AdminAuditEntry.CreatedAtUtc),
                    nameof(AdminAuditEntry.Id)
                ]));
    }
}
