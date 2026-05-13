//-------------------------------------------------------------------------------------------------
//
// IRequestHandler.cs -- The IRequestHandler interface.
//
// Copyright (c) 2026 JBT Marel. All rights reserved.
//
//-------------------------------------------------------------------------------------------------

namespace Arelia.Application.Mediator;

/// <summary>
/// Handles a mediator request of type <typeparamref name="TRequest"/> and returns a response of
/// type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request to handle.</typeparam>
/// <typeparam name="TResponse">The type of response produced.</typeparam>
public interface IRequestHandler<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	//---------------------------------------------------------------------------------------------
	/// <summary>
	/// Handles the specified request.
	/// </summary>
	/// <param name="request">The request to handle.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>The response produced by handling the request.</returns>
	Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
