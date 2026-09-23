using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace AtaraxiaAI.Business;

public static class DependencyInjection
{
    /// <summary>
    /// Registers internal business-tier services.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection RegisterInternalBusinessServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);

        return services;
    }
}
