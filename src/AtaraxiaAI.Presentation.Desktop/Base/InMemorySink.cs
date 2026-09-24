using Avalonia.Threading;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace AtaraxiaAI.Presentation.Desktop.Base;

public sealed record LogEntry(string Text, bool IsError);

public class InMemorySink : ILogEventSink
{
    private readonly MessageTemplateTextFormatter _formatter = new(
        "{Timestamp:HH:mm:ss} {Level:u3} | {Message:lj}{NewLine}{Exception}", CultureInfo.InvariantCulture);

    public ObservableCollection<LogEntry> Messages { get; } = new();

    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        var entry = new LogEntry(writer.ToString(), logEvent.Level >= LogEventLevel.Error);
        if (Dispatcher.UIThread.CheckAccess())
            Messages.Add(entry);
        else
            Dispatcher.UIThread.Post(() => Messages.Add(entry));
    }
}
