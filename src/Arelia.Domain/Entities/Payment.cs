using Arelia.Domain.Common;

namespace Arelia.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid? ChargeId { get; set; }
    public Guid? PayerPersonId { get; set; }
    public string PayerDescription { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal? OriginalAmount { get; set; }
    public string? OriginalCurrencyCode { get; set; }

    // Navigation
    public Charge? Charge { get; set; }
    public Person? PayerPerson { get; set; }
}
