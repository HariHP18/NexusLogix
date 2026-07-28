namespace NexusLogix.Domain.Common;

public interface IMustHaveTenant
{
    public string TenantId { get; set; }
}