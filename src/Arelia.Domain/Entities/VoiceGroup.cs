using Arelia.Domain.Common;

namespace Arelia.Domain.Entities;

public class VoiceGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    // Navigation
    public ICollection<Person> People { get; set; } = [];
}
