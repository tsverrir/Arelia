namespace Arelia.Domain.Common;

/// <summary>
/// Base class for all domain events raised by domain entities.
/// </summary>
public abstract class DomainEvent
{
	//---------------------------------------------------------------------------------------------
	/// <summary>
	/// Gets the UTC date and time at which the event occurred.
	/// </summary>
	public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
