using Arelia.Domain.Common;
using Arelia.Domain.Enums;

namespace Arelia.Domain.Entities;

public class ChargeLine : BaseEntity
{
    public Guid ChargeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ChargeLineType LineType { get; set; }
    public bool IsSelected { get; set; } = true;

    // Navigation
    public Charge Charge { get; set; } = null!;
}
