using AtaraxiaAI.ViewModels;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace AtaraxiaAI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = App.Current?.Services?.GetService<MainWindowViewModel>();
        }
    }
}
