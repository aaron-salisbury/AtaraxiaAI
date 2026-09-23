using AtaraxiaAI.Presentation.Desktop.Base;
using AtaraxiaAI.Presentation.Desktop.ViewModels;
using AtaraxiaAI.Presentation.Desktop.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;

namespace AtaraxiaAI.Presentation.Desktop;

public partial class App : Application
{
    public static InMemorySink InMemorySink { get; } = new InMemorySink();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainViewModel mainViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = mainViewModel };
            desktop.Exit += (_, _) => mainViewModel.Shutdown();
        }
        else
        {
            throw new NotSupportedException("Only classic desktop lifetime is supported.");
        }

        base.OnFrameworkInitializationCompleted();
    }
}
