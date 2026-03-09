using Arelia.Domain.Common;
using Arelia.Domain.Enums;

namespace Arelia.Domain.Entities;

public class ActivityParticipant : BaseEntity
{
    public Guid ActivityId { get; set; }
    public Guid PersonId { get; set; }
    public RsvpStatus RsvpStatus { get; set; } = RsvpStatus.Unanswered;
    public SignupStatus SignupStatus { get; set; } = SignupStatus.None;
    public int? WaitlistPosition { get; set; }
    public DateTime? RsvpTimestamp { get; set; }

    // Navigation
    public Activity Activity { get; set; } = null!;
    public Person Person { get; set; } = null!;
}
