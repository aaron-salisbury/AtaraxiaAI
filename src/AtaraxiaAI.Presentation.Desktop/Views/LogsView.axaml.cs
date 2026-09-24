using AtaraxiaAI.Presentation.Desktop.ViewModels;
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Specialized;

namespace AtaraxiaAI.Presentation.Desktop.Views;

public partial class LogsView : UserControl
{
    private LogsViewModel? _currentModel;

    public LogsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_currentModel is not null)
            _currentModel.Messages.CollectionChanged -= OnMessagesChanged;
        _currentModel = DataContext as LogsViewModel;
        if (_currentModel is { } model)
        {
            model.Messages.CollectionChanged += OnMessagesChanged;
            ScrollToNewest();
        }
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e) => ScrollToNewest();

    private void ScrollToNewest() => Dispatcher.UIThread.Post(() => LogScroller.ScrollToEnd(), DispatcherPriority.Loaded);
}
