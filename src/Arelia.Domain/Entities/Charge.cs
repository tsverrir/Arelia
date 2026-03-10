using Arelia.Domain.Common;
using Arelia.Domain.Enums;

namespace Arelia.Domain.Entities;

public class Charge : BaseEntity
{
    public Guid PersonId { get; set; }
    public Guid? SemesterId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public ChargeStatus Status { get; set; } = ChargeStatus.Open;
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Points to the new revision that replaced this one (null if current).</summary>
    public Guid? ReplacedById { get; set; }

    /// <summary>Points to the first version in this chain (null if this is the original).</summary>
    public Guid? OriginalId { get; set; }

    // Navigation
    public Person Person { get; set; } = null!;
    public Activity? Semester { get; set; }
    public Charge? ReplacedBy { get; set; }
    public Charge? Original { get; set; }
    public ICollection<ChargeLine> ChargeLines { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];

    public decimal TotalDue => ChargeLines.Where(cl => cl.IsSelected && cl.IsActive).Sum(cl => cl.Amount);
    public decimal TotalPaid => Payments.Where(p => p.IsActive).Sum(p => p.Amount);

    public void RecalculateStatus()
    {
        var due = TotalDue;
        var paid = TotalPaid;

        Status = paid switch
        {
            0 when due > 0 => ChargeStatus.Open,
            _ when paid >= due && due > 0 => paid > due ? ChargeStatus.Overpaid : ChargeStatus.Paid,
            _ when paid > 0 && paid < due => ChargeStatus.PartiallyPaid,
            _ => ChargeStatus.Open
        };
    }
}
