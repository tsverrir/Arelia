using Arelia.Domain.Common;
using Arelia.Domain.Enums;

namespace Arelia.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Identifies whether this is a System Role (Admin/Board/Member) or a Custom org-defined role.
    /// Admin permissions are hard-coded; Board and Member ship with editable defaults.
    /// </summary>
    public RoleType RoleType { get; set; } = RoleType.Custom;

    // Navigation
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<RoleAssignment> RoleAssignments { get; set; } = [];
}
