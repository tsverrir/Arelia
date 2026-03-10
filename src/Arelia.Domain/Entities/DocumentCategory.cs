using Arelia.Domain.Common;

namespace Arelia.Domain.Entities;

public class DocumentCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    // Navigation
    public ICollection<Document> Documents { get; set; } = [];
}
