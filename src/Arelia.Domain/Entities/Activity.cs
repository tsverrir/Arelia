using Arelia.Domain.Common;
using Arelia.Domain.Enums;

namespace Arelia.Domain.Entities;

public class Activity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ActivityType ActivityType { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Location { get; set; }
    public Guid? ParentActivityId { get; set; }
    public int WorkYear { get; set; }
    public bool IsPublicVisible { get; set; }
    public int? MaxCapacity { get; set; }
    public DateTime? SignupDeadline { get; set; }

    /// <summary>
    /// When true, all active members are assumed to be participants (e.g. rehearsals).
    /// When false, participants must be added explicitly.
    /// </summary>
    public bool IsImplicitParticipation { get; set; }

    // Navigation
    public Activity? ParentActivity { get; set; }
    public ICollection<Activity> ChildActivities { get; set; } = [];
    public ICollection<ActivityParticipant> Participants { get; set; } = [];
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];
}
