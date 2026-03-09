using Arelia.Domain.Common;
using Arelia.Domain.Enums;

namespace Arelia.Domain.Entities;

public class AttendanceRecord : BaseEntity
{
    public Guid ActivityId { get; set; }
    public Guid PersonId { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public string? Comment { get; set; }
    public string? RecordedByUserId { get; set; }
    public DateTime RecordedAt { get; set; }

    // Navigation
    public Activity Activity { get; set; } = null!;
    public Person Person { get; set; } = null!;
}
