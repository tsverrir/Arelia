//-------------------------------------------------------------------------------------------------
//
// IPipelineBehavior.cs -- The IPipelineBehavior interface.
//
// Copyright (c) 2026 JBT Marel. All rights reserved.
//
//-------------------------------------------------------------------------------------------------

namespace Arelia.Application.Mediator;

/// <summary>
/// A pipeline behavior that wraps the inner handler for cross-cutting concerns such as validation,
/// logging, or performance monitoring.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response produced.</typeparam>
public interface IPipelineBehavior<TRequest, TResponse>
	where TRequest : notnull
{
	//---------------------------------------------------------------------------------------------
	/// <summary>
	/// Handles the request, optionally performing work before and after calling the next delegate
	/// in the pipeline.
	/// </summary>
	/// <param name="request">The current request.</param>
	/// <param name="next">The delegate representing the remainder of the pipeline.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>The response from the handler or a downstream pipeline step.</returns>
	Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
