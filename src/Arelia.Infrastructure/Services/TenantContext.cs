using Arelia.Application.Interfaces;

namespace Arelia.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    public Guid? CurrentOrganizationId { get; private set; }
    public string? CurrentUserId { get; set; }

    public void SetOrganization(Guid organizationId)
    {
        CurrentOrganizationId = organizationId;
    }
}
