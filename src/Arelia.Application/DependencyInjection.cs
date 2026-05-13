using Arelia.Application.Authorization;
using Arelia.Application.Mediator;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Arelia.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Register all IRequestHandler<TRequest, TResponse> implementations from the assembly.
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .Select(i => (Implementation: t, Interface: i)))
            .ToList();

        foreach (var (impl, iface) in handlerTypes)
        {
            services.AddTransient(iface, impl);
        }

        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IMediator, Mediator.Mediator>();
        services.AddScoped<IPermissionService, PermissionService>();

        return services;
    }
}
