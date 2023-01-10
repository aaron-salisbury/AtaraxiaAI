using Avalonia;
using Serilog;
using System;

namespace AtaraxiaAI
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly); // Needed for SVG control to work with Avalonia previewer.

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(App.InMemorySink)
                .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 1)
                .CreateLogger();

            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        }
    }
}
