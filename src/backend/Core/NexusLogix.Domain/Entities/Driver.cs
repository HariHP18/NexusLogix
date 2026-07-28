using NexusLogix.Domain.Common;

namespace NexusLogix.Domain.Entities;


public class Driver : IMustHaveTenant
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Available"; // e.g., Available, EnRoute, OffDuty

    // Multi-tenant tracking (We will use this in Phase 3)
    public string TenantId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}