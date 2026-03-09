using Arelia.Domain.Common;

namespace Arelia.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<RoleAssignment> RoleAssignments { get; set; } = [];
}
