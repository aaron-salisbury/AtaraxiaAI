using AtaraxiaAI.ViewModels;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace AtaraxiaAI.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();

            DataContext = App.Current?.Services?.GetService<SettingsViewModel>();
        }
    }
}
