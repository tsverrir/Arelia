using Arelia.Domain.Common;
using Arelia.Domain.Enums;

namespace Arelia.Domain.Entities;

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Permission Permission { get; set; }

    // Navigation
    public Role Role { get; set; } = null!;
}
