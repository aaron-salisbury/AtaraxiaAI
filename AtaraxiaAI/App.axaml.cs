using AtaraxiaAI.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace AtaraxiaAI
{
    public partial class App : Application
    {
        public new static App? Current => Application.Current as App;

        public IServiceProvider? Services { get; set; }

        public override void Initialize()
        {
            Services = ConfigureServices();
            AvaloniaXamlLoader.Load(this);
        }

        private static IServiceProvider ConfigureServices()
        {
            // https://docs.microsoft.com/en-us/windows/communitytoolkit/mvvm/ioc
            ServiceCollection services = new ServiceCollection();

            // Make IHttpClientFactory available to application.
            // HttpClient instances created by IHttpClientFactory are intended to be short-lived.
            // Disposing of such HttpClient instances created by the factory will not lead to socket exhaustion.
            // AddHttpClient() is in the Microsoft.Extensions.Http nuget package.
            services.AddHttpClient(); 

            // This app requires the naming convention that views end in "View" (Base.ViewLocator.cs),
            // ViewModels end in "ViewModel", and that nothing else ends in either.
            foreach (Type appType in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (appType.Name.EndsWith("ViewModel"))
                {
                    services.AddSingleton(appType);
                }
            }

            return services.BuildServiceProvider();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
