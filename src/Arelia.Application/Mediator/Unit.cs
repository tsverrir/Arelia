//-------------------------------------------------------------------------------------------------
//
// Unit.cs -- The Unit struct.
//
// Copyright (c) 2026 JBT Marel. All rights reserved.
//
//-------------------------------------------------------------------------------------------------

namespace Arelia.Application.Mediator;

/// <summary>
/// Represents a void return value for commands that do not produce a meaningful result.
/// Use <see cref="Value"/> as the return value in handlers that would otherwise return
/// <see cref="Task"/> without a result.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
	//---------------------------------------------------------------------------------------------
	/// <summary>Gets the singleton value of <see cref="Unit"/>.</summary>
	public static readonly Unit Value = default;

	//---------------------------------------------------------------------------------------------
	/// <inheritdoc />
	public bool Equals(Unit other) => true;

	//---------------------------------------------------------------------------------------------
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Unit;

	//---------------------------------------------------------------------------------------------
	/// <inheritdoc />
	public override int GetHashCode() => 0;

	//---------------------------------------------------------------------------------------------
	/// <inheritdoc />
	public override string ToString() => "()";
}
