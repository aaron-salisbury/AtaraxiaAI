using AtaraxiaAI.Presentation.Desktop;
using AtaraxiaAI.Integrations;
using Avalonia;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

namespace AtaraxiaAI.DesktopApp;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 3 && args[0] == "--kokoro-worker")
        {
            try { KokoroWorker.SynthesizeToFileAsync(args[1], args[2]).GetAwaiter().GetResult(); }
            catch (Exception error)
            {
                try { System.IO.File.WriteAllText(args[2] + ".error", error.ToString()); }
                catch (System.IO.IOException) { }
                Environment.ExitCode = 1;
            }
            return;
        }

        ServiceProvider? serviceProvider = null;

        try
        {
            IServiceCollection services = DependencyInjection.BuildServiceCollection();
            serviceProvider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(serviceProvider);

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            serviceProvider?.Dispose();
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
