namespace Arelia.Domain.Entities;

public class OrganizationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public Guid? PersonId { get; set; }
    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public Organization Organization { get; set; } = null!;
    public Person? Person { get; set; }
}
