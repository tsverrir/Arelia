using Arelia.Domain.Common;

namespace Arelia.Domain.Entities;

public class Person : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? VoiceGroupId { get; set; }
    public string? Notes { get; set; }

    /// <summary>True once a person has been permanently deleted via the Delete action. Cannot be undone.</summary>
    public bool IsDeleted { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    // Navigation
    public VoiceGroup? VoiceGroup { get; set; }
    public ICollection<RoleAssignment> RoleAssignments { get; set; } = [];
}
