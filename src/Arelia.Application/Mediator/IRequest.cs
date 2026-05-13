//-------------------------------------------------------------------------------------------------
//
// IRequest.cs -- The IRequest interface.
//
// Copyright (c) 2026 JBT Marel. All rights reserved.
//
//-------------------------------------------------------------------------------------------------

namespace Arelia.Application.Mediator;

/// <summary>
/// Marker interface for mediator requests that produce a response of type
/// <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the request handler.</typeparam>
public interface IRequest<TResponse>
{
}
