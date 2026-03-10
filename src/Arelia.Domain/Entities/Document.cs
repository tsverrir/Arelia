using Arelia.Domain.Common;

namespace Arelia.Domain.Entities;

public class Document : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public Guid? DocumentCategoryId { get; set; }

    // Navigation
    public DocumentCategory? Category { get; set; }
}
