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

    /// <summary>Points to the new revision that replaced this one (null if current).</summary>
    public Guid? ReplacedById { get; set; }

    /// <summary>Points to the first version in this chain (null if this is the original).</summary>
    public Guid? OriginalId { get; set; }

    // Navigation
    public ExpenseCategory Category { get; set; } = null!;
    public Expense? ReplacedBy { get; set; }
    public Expense? Original { get; set; }
    public ICollection<ExpenseAttachment> Attachments { get; set; } = [];
}
