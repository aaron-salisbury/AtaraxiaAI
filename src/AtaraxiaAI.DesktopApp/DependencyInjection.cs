using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Integrations;
using AtaraxiaAI.Presentation.Desktop;
using AtaraxiaAI.Presentation.Desktop.Base.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunnethOverStudio.AppToolkit.Modules.Access;
using RunnethOverStudio.AppToolkit.Modules.Messaging;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace AtaraxiaAI.DesktopApp;

internal static class DependencyInjection
{
    // Logs, desktop preferences, and downloaded speech models are kept here.
    private static readonly string APP_DATA_DIRECTORY =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AtaraxiaAI");

    internal static IServiceCollection BuildServiceCollection()
    {
        Directory.CreateDirectory(APP_DATA_DIRECTORY);
        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(App.InMemorySink)
            .WriteTo.File(Path.Combine(APP_DATA_DIRECTORY, "log-.txt"), rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 1)
            .CreateLogger();

        IServiceCollection services = new ServiceCollection();

        // Infrastructure
        services.AddLogging(configure => configure.AddSerilog(Serilog.Log.Logger))
            .AddSingleton((sp) => sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(App)))
            .AddSingleton<IEventSystem, EventSystem>();

        // File and web access
        services.ComposeAccessIntegrations();

        // Business
        services.RegisterInternalBusinessServices()
            .ComposeBusinessIntegrations();

        // Presentation
        services.RegisterInternalPresentationServices(APP_DATA_DIRECTORY)
            .ComposePresentationIntegrations();

        return services;
    }

    private static IServiceCollection ComposeAccessIntegrations(this IServiceCollection services)
    {
        // File System Access
        services.AddScoped<IFileSystemAccess, FileSystemAccess>();

        // Web Access
        services.AddHttpClient(HttpRequester.COMPRESSION_CLIENT_NAME, c => c.DefaultRequestHeaders.Add("Accept-Encoding", "deflate, gzip"))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            });
        services.AddSingleton<IHttpRequester, HttpRequester>();

        return services;
    }

    private static IServiceCollection ComposeBusinessIntegrations(this IServiceCollection services)
    {
        services.RegisterInternalIntegrationsServices(APP_DATA_DIRECTORY);
        services.AddSingleton(sp => new IntegrationDependencies(sp.GetRequiredService<IHttpRequester>(), Log.Logger));
        services.AddSingleton<IAudioPlayer, WavAudioPlayer>();
        services.AddSingleton(sp => new AI(
            Log.Logger,
            sp.GetRequiredService<IIntegrationFactory>(),
            sp.GetRequiredService<IAudioPlayer>(),
            sp.GetRequiredService<IntegrationDependencies>()));

        return services;
    }

    private static IServiceCollection ComposePresentationIntegrations(this IServiceCollection services)
    {
        services.AddScoped<IAgnosticDispatcher, AvaloniaDispatcher>();

        return services;
    }

}
