using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PaymentFlow.Application.Common.Behaviors;

namespace PaymentFlow.Application;

/// <summary>
/// Registers everything the Application layer owns: MediatR handlers and
/// pipeline behaviors, and FluentValidation validators. Kept as a single
/// extension method so Program.cs (in the Api and Workers projects) only
/// ever needs to call builder.Services.AddApplication() — it never needs
/// to know what's actually inside this layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            // Order matters: behaviors run in the order they're registered.
            // Logging wraps everything (including validation failures);
            // Validation runs next, short-circuiting before the real handler.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}