using Arelia.Domain.Common;

namespace Arelia.Domain.Entities;

public class CreditTransaction : BaseEntity
{
    public Guid PersonId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? ChargeId { get; set; }

    // Navigation
    public Person Person { get; set; } = null!;
    public Payment? Payment { get; set; }
    public Charge? Charge { get; set; }
}
