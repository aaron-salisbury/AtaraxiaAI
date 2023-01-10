using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Specialized;

namespace AtaraxiaAI.ViewModels
{
    public partial class LogsViewModel : ObservableObject
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
}
