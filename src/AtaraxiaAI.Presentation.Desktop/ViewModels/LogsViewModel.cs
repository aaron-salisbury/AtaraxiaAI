using AtaraxiaAI.Presentation.Desktop.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using System.Collections.Specialized;

namespace AtaraxiaAI.Presentation.Desktop.ViewModels;

public partial class LogsViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _logMessages;

    public LogsViewModel()
    {
        _logMessages = string.Empty;

        App.InMemorySink.Messages.CollectionChanged += new NotifyCollectionChangedEventHandler(OnLogsPropertyChanged);
    }

    private void OnLogsPropertyChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        LogMessages = string.Concat(App.InMemorySink.Messages);
    }
}
