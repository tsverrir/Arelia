//-------------------------------------------------------------------------------------------------
//
// RequestHandlerDelegate.cs -- The RequestHandlerDelegate delegate.
//
// Copyright (c) 2026 JBT Marel. All rights reserved.
//
//-------------------------------------------------------------------------------------------------

namespace Arelia.Application.Mediator;

/// <summary>
/// Represents the next handler in a pipeline behavior chain, or the final request handler.
/// </summary>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <param name="cancellationToken">A token to cancel the operation.</param>
/// <returns>The response from the next handler in the pipeline.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);
