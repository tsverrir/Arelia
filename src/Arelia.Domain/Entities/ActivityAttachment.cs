using Arelia.Domain.Common;

namespace Arelia.Domain.Entities;

public class ActivityAttachment : BaseEntity
{
    public Guid ActivityId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }

    // Navigation
    public Activity Activity { get; set; } = null!;
}
