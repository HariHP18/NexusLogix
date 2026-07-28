using Microsoft.EntityFrameworkCore;
using NexusLogix.Domain.Entities;

namespace NexusLogix.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Driver> Drivers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}