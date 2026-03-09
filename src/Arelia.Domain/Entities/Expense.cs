using Arelia.Domain.Common;

namespace Arelia.Domain.Entities;

public class Expense : BaseEntity
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public Guid CategoryId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? ReceivedBy { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ExpenseCategory Category { get; set; } = null!;
    public ICollection<ExpenseAttachment> Attachments { get; set; } = [];
}
