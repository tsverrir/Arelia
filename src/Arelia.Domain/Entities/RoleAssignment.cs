using Arelia.Domain.Common;

namespace Arelia.Domain.Entities;

public class RoleAssignment : BaseEntity
{
    public Guid PersonId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public bool IsCurrentlyActive =>
        FromDate <= DateTime.UtcNow && (ToDate == null || ToDate >= DateTime.UtcNow);

    // Navigation
    public Person Person { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
