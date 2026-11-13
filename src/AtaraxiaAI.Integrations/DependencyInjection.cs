using AtaraxiaAI.Business.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.Http;

namespace AtaraxiaAI.Integrations;

public static class DependencyInjection
{
    /// <summary>
    /// Registers internal integrations-tier services. <see cref="IntegrationDependencies"/> and
    /// <see cref="IHttpClientFactory"/> are registered by the composition root.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="applicationDataDirectory">The directory under which downloaded speech models are stored.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection RegisterInternalIntegrationsServices(this IServiceCollection services, string applicationDataDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDataDirectory);

        services.AddSingleton<IIntegrationFactory>(provider => new IntegrationFactory(
            provider.GetRequiredService<IntegrationDependencies>(),
            provider.GetRequiredService<IHttpClientFactory>(),
            Path.Combine(applicationDataDirectory, "Speech")));

        return services;
    }
}
