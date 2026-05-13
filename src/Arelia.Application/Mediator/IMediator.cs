//-------------------------------------------------------------------------------------------------
//
// IMediator.cs -- The IMediator interface.
//
// Copyright (c) 2026 JBT Marel. All rights reserved.
//
//-------------------------------------------------------------------------------------------------

namespace Arelia.Application.Mediator;

/// <summary>
/// Dispatches a request to its corresponding handler through the configured pipeline behaviors.
/// </summary>
public interface IMediator
{
	//---------------------------------------------------------------------------------------------
	/// <summary>
	/// Sends the specified request to its single registered handler.
	/// </summary>
	/// <typeparam name="TResponse">The type of response expected.</typeparam>
	/// <param name="request">The request to send.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>The response from the handler.</returns>
	Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
