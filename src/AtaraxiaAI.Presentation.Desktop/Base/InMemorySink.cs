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
    public const int DefaultCapacity = 500;

    private readonly MessageTemplateTextFormatter _formatter = new(
        "{Timestamp:HH:mm:ss} {Level:u3} | {Message:lj}{NewLine}{Exception}", CultureInfo.InvariantCulture);
    private readonly int _capacity;

    public InMemorySink(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
    }

    /// <summary>The most recent log entries, oldest first. Older entries are dropped beyond the capacity.</summary>
    public ObservableCollection<LogEntry> Messages { get; } = new();

    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        // The template separates the message from any exception with a newline; trim the one left at the end.
        var entry = new LogEntry(writer.ToString().TrimEnd(), logEvent.Level >= LogEventLevel.Error);
        if (Dispatcher.UIThread.CheckAccess())
            Add(entry);
        else
            Dispatcher.UIThread.Post(() => Add(entry));
    }

    private void Add(LogEntry entry)
    {
        Messages.Add(entry);
        while (Messages.Count > _capacity)
            Messages.RemoveAt(0);
    }
}
