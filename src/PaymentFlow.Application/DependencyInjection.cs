using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

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
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}