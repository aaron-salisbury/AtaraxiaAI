using Avalonia.Controls;
using Avalonia.Platform.Storage;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AtaraxiaAI.Presentation.Desktop.Base.Extensions;

internal static class UserControlExtensions
{
    internal static async Task<IStorageFile?> GetUserSelectedFileAsync(this UserControl view, string startingFolderPath, string title, params FilePickerFileType[] filePickerTypes)
    {
        IStorageFile? userSelectedFile = null;
        TopLevel? topLevel = TopLevel.GetTopLevel(view);

        if (topLevel != null)
        {
            FilePickerOpenOptions filePickerOpenOptions = new()
            {
                Title = title,
                AllowMultiple = false,
                SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri(startingFolderPath)),
                FileTypeFilter = filePickerTypes
            };

            IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(filePickerOpenOptions);

            if (files.Count > 0)
            {
                userSelectedFile = files[0];
            }
        }

        return userSelectedFile;
    }

    internal static void LoadModelEvents(this UserControl view)
    {
        if (view.DataContext is BaseViewModel viewModel)
        {
            viewModel.AddModelEvents();
        }
    }

    internal static void UnloadModelEvents(this UserControl view)
    {
        if (view.DataContext is BaseViewModel viewModel)
        {
            viewModel.RemoveModelEvents();
        }
    }
}
