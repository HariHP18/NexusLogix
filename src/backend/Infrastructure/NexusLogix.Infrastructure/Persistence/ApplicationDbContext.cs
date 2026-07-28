using Microsoft.EntityFrameworkCore;
using NexusLogix.Application.Common.Interfaces;
using NexusLogix.Domain.Entities;
using NexusLogix.Domain.Common;
using NexusLogix.Infrastructure.Interfaces;

namespace NexusLogix.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantProvider _tenantProvider;

    // Injecting ITenantProvider alongside the standard DbContextOptions
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantProvider tenantProvider) : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Driver> Drivers => Set<Driver>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Fluent API rules (Kept exactly as we built them before)
        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LicenseNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TenantId).HasMaxLength(50).IsRequired();

            // 2. The Sandbox Filter: Append the automatic WHERE clause rule
            entity.HasQueryFilter(d => d.TenantId == _tenantProvider.TenantId);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 3. Automated Stamping: Inject active Tenant ID whenever saving a brand-new entity
        foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _tenantProvider.TenantId;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}