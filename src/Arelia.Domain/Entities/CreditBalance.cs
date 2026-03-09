using Arelia.Domain.Common;

namespace Arelia.Domain.Entities;

public class CreditBalance : BaseEntity
{
    public Guid PersonId { get; set; }
    public decimal BalanceAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;

    // Navigation
    public Person Person { get; set; } = null!;
    public ICollection<CreditTransaction> Transactions { get; set; } = [];
}
