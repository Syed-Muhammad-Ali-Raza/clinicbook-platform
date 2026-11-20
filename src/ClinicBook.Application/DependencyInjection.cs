using System.Reflection;
using ClinicBook.Application.Common.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicBook.Application;

/// <summary>
/// Dependency injection registration for the Application layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adds Application layer services.</summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
