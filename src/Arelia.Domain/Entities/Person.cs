using Arelia.Domain.Common;
using Arelia.Domain.Enums;

namespace Arelia.Domain.Entities;

public class Person : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public VoiceGroup? VoiceGroup { get; set; }
    public string? Notes { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    // Navigation
    public ICollection<RoleAssignment> RoleAssignments { get; set; } = [];
}
