using AtaraxiaAI.Presentation.Desktop.Settings;
using AtaraxiaAI.Presentation.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AtaraxiaAI.Presentation.Desktop;

public static class DependencyInjection
{
    /// <param name="services">The service collection.</param>
    /// <param name="applicationDataDirectory">The directory where desktop preferences are kept.</param>
    public static IServiceCollection RegisterInternalPresentationServices(this IServiceCollection services, string applicationDataDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDataDirectory);

        services.AddSingleton<IDesktopSettings>(_ => new DesktopSettings(applicationDataDirectory));
        services.AddSingleton<LogsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<VisionFeedViewModel>();
        services.AddSingleton<MainViewModel>();

        return services;
    }
}
