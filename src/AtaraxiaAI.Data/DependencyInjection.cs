using Microsoft.Extensions.DependencyInjection;
using System;

namespace AtaraxiaAI.Data;

public static class DependencyInjection
{
    /// <summary>
    /// Registers internal data-tier services.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection RegisterInternalDataServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register application-owned persistence services here.

        return services;
    }
}
