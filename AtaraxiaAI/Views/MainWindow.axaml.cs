using AtaraxiaAI.ViewModels;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace AtaraxiaAI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = App.Current?.Services?.GetService<MainWindowViewModel>();

            this.Closing += OnClosing;
        }

        private void OnClosing(object? s, CancelEventArgs a)
        {
            if (DataContext != null && DataContext is MainWindowViewModel viewModel)
            {
                viewModel.Shutdown();
            }
        }
    }
}
