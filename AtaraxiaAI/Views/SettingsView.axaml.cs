using AtaraxiaAI.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;

namespace AtaraxiaAI.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();

            DataContext = App.Current?.Services?.GetService<SettingsViewModel>();

            Button storageSelectionBtn = this.FindControl<Button>("StorageLocationBtn");
            storageSelectionBtn.Click += OnSelectFolderClick;
        }

        private void OnSelectFolderClick(object? sender, RoutedEventArgs a)
        {
            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                OpenFolderDialog dialog = new OpenFolderDialog();
                string? result = dialog.ShowAsync(desktop.MainWindow).Result;

                if (!string.IsNullOrEmpty(result) && DataContext is SettingsViewModel viewModel)
                {
                    viewModel.UserStorageDirectory = result;
                }
            }
        }
    }
}
