using AtaraxiaAI.ViewModels;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace AtaraxiaAI.Views
{
    public partial class LogsView : UserControl
    {
        public LogsView()
        {
            InitializeComponent();

            DataContext = App.Current?.Services?.GetService<LogsViewModel>();
        }
    }
}
