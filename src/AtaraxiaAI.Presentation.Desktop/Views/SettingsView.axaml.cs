using AtaraxiaAI.Presentation.Desktop.Base.Extensions;
using AtaraxiaAI.Presentation.Desktop.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using Serilog;

namespace AtaraxiaAI.Presentation.Desktop.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();


        Button storageSelectionBtn = this.FindControl<Button>("StorageLocationBtn") ?? throw new InvalidOperationException("StorageLocationBtn not found");
        storageSelectionBtn.Click += OnSelectFolderClick;
    }

    private async void OnSelectFolderClick(object? sender, RoutedEventArgs a)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            IStorageFolder? selectedFolder = await this.GetUserSelectedFolderAsync(viewModel.UserStorageDirectory);

            if (selectedFolder?.Path is { IsFile: true } folderPath)
            {
                try
                {
                    await viewModel.ChangeUserStorageDirectoryAsync(folderPath.LocalPath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to update the user storage directory.");
                }
            }
        }
    }
}
