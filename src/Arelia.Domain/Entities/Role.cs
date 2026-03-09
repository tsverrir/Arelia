using Arelia.Domain.Common;
using Arelia.Domain.Enums;

namespace Arelia.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public RoleType RoleType { get; set; }

    // Navigation
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<RoleAssignment> RoleAssignments { get; set; } = [];
}
