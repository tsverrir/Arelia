using Arelia.Domain.Common;

namespace Arelia.Domain.Entities;

public class RehearsalRecurrenceTemplate : BaseEntity
{
    public Guid SemesterId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? Location { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Navigation
    public Activity Semester { get; set; } = null!;
}
