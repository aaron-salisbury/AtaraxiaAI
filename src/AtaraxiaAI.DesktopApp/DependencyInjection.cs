using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Persistence;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Data;
using AtaraxiaAI.Integrations;
using AtaraxiaAI.Presentation.Desktop;
using AtaraxiaAI.Presentation.Desktop.Base.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunnethOverStudio.AppToolkit.Modules.Access;
using RunnethOverStudio.AppToolkit.Modules.Messaging;
using Serilog;
using System.Net;
using System.Net.Http;

namespace AtaraxiaAI.DesktopApp;

internal static class DependencyInjection
{
    internal static IServiceCollection BuildServiceCollection()
    {
        var fileLog = new RelocatableLogSink();
        fileLog.UseDirectory(new JsonAppDataStore().ReadInternalStorageAsync()
            .GetAwaiter().GetResult().UserStorageDirectory);
        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(App.InMemorySink)
            .WriteTo.Sink(fileLog)
            .CreateLogger();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton<IAppLogLocation>(fileLog);

        // Infrastructure
        services.AddLogging(configure => configure.AddSerilog(Serilog.Log.Logger))
            .AddSingleton((sp) => sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(App)))
            .AddSingleton<IEventSystem, EventSystem>();

        // Data Access
        services.RegisterInternalDataServices()
            .ComposeDataAccessIntegrations();

        // Business
        services.RegisterInternalBusinessServices()
            .ComposeBusinessIntegrations();

        // Presentation
        services.RegisterInternalPresentationServices()
            .ComposePresentationIntegrations();

        return services;
    }

    private static IServiceCollection ComposeDataAccessIntegrations(this IServiceCollection services)
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

        // Database Access
        services.AddSingleton<IAppDataStore, JsonAppDataStore>();

        return services;
    }

    private static IServiceCollection ComposeBusinessIntegrations(this IServiceCollection services)
    {
        services.AddSingleton(sp => new IntegrationDependencies(
            sp.GetRequiredService<IHttpRequester>(), Log.Logger));
        services.AddSingleton<IIntegrationFactory, IntegrationFactory>();
        services.AddSingleton<IAudioPlayer, WavAudioPlayer>();
        services.AddSingleton(sp => new AI(
            Log.Logger,
            sp.GetRequiredService<IIntegrationFactory>(),
            sp.GetRequiredService<IAppDataStore>(),
            sp.GetRequiredService<IAudioPlayer>(),
            sp.GetRequiredService<IntegrationDependencies>(),
            sp.GetRequiredService<IAppLogLocation>()));

        return services;
    }

    private static IServiceCollection ComposePresentationIntegrations(this IServiceCollection services)
    {
        services.AddScoped<IAgnosticDispatcher, AvaloniaDispatcher>();

        return services;
    }

}
