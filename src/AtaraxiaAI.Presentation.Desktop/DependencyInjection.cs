using AtaraxiaAI.Presentation.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AtaraxiaAI.Presentation.Desktop;

public static class DependencyInjection
{
    public static IServiceCollection RegisterInternalPresentationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<LogsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<VisionFeedViewModel>();
        services.AddSingleton<MainViewModel>();

        return services;
    }
}
