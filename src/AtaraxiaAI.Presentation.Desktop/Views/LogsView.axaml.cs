using AtaraxiaAI.Presentation.Desktop.ViewModels;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace AtaraxiaAI.Presentation.Desktop;

public partial class LogsView : UserControl
{
    public LogsView()
    {
        InitializeComponent();

        DataContext = Ioc.Default.GetService<LogsViewModel>();
    }
}
