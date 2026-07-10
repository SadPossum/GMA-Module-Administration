namespace Gma.Modules.Administration.Persistence;

using Gma.Modules.Administration.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public DbSet<AdminAuditEntry> AuditEntries => this.Set<AdminAuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(AdminMigrations.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);
    }
}
