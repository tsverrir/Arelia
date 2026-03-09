using Arelia.Domain.Common;

namespace Arelia.Domain.Entities;

public class ExpenseAttachment : BaseEntity
{
    public Guid ExpenseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }

    // Navigation
    public Expense Expense { get; set; } = null!;
}
