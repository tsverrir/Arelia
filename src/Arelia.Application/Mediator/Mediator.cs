//-------------------------------------------------------------------------------------------------
//
// Mediator.cs -- The Mediator class.
//
// Copyright (c) 2026 JBT Marel. All rights reserved.
//
//-------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Arelia.Application.Mediator;

/// <summary>
/// In-process mediator that resolves handlers and behaviors from the DI container and executes
/// the pipeline for the given request.
/// </summary>
public sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
	//---------------------------------------------------------------------------------------------
	/// <inheritdoc />
	public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(request.GetType(), typeof(TResponse));
		var wrapper = (IRequestHandlerWrapper<TResponse>)Activator.CreateInstance(wrapperType)!;
		return wrapper.Handle(request, serviceProvider, cancellationToken);
	}

	//---------------------------------------------------------------------------------------------
	/// <summary>Internal abstraction for invoking typed handlers and pipeline behaviors via reflection.</summary>
	private interface IRequestHandlerWrapper<TResponse>
	{
		Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider sp, CancellationToken cancellationToken);
	}

	//---------------------------------------------------------------------------------------------
	private sealed class RequestHandlerWrapper<TRequest, TResponse> : IRequestHandlerWrapper<TResponse>
		where TRequest : IRequest<TResponse>
	{
		public Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider sp, CancellationToken cancellationToken)
		{
			var handler = sp.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
			var behaviors = sp.GetServices<IPipelineBehavior<TRequest, TResponse>>()
				.Reverse()
				.ToList();

			RequestHandlerDelegate<TResponse> pipeline = token => handler.Handle((TRequest)request, token);

			foreach (var behavior in behaviors)
			{
				var next = pipeline;
				pipeline = token => behavior.Handle((TRequest)request, next, token);
			}

			return pipeline(cancellationToken);
		}
	}
}
