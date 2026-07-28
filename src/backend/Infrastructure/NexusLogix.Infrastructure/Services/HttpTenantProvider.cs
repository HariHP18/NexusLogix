using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NexusLogix.Infrastructure.Interfaces;

namespace NexusLogix.Infrastructure.Services;

public class HttpTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string TenantId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;

            // Extract the tenant_id claim embedded directly inside the JWT token
            var tenantClaim = user?.FindFirst("tenant_id")?.Value;

            if (!string.IsNullOrEmpty(tenantClaim))
            {
                return tenantClaim;
            }

            // Fallback to header for inter-service communication if present
            var tenantHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].ToString();

            return !string.IsNullOrEmpty(tenantHeader) ? tenantHeader : "default-tenant";
        }
    }
}