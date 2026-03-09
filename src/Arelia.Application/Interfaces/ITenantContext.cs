namespace Arelia.Application.Interfaces;

public interface ITenantContext
{
    Guid? CurrentOrganizationId { get; }
    string? CurrentUserId { get; }
    void SetOrganization(Guid organizationId);
}
