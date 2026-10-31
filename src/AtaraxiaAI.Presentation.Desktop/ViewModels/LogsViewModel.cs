using AtaraxiaAI.Presentation.Desktop.Base;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using System.Collections.ObjectModel;

namespace AtaraxiaAI.Presentation.Desktop.ViewModels;

public partial class LogsViewModel : BaseViewModel
{
    public ObservableCollection<LogEntry> Messages => App.InMemorySink.Messages;
}
