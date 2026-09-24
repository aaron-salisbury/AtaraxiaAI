using AtaraxiaAI.Business.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;

namespace AtaraxiaAI.DesktopApp;

internal sealed class RelocatableLogSink : ILogEventSink, IAppLogLocation, IDisposable
{
    private readonly object _gate = new();
    private ILogger? _fileLogger;

    public void UseDirectory(string directory)
    {
        string fullDirectory = Path.GetFullPath(directory);
        Directory.CreateDirectory(fullDirectory);
        var replacement = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(Path.Combine(fullDirectory, "log-.txt"), rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 1)
            .CreateLogger();

        lock (_gate)
        {
            var previous = _fileLogger;
            _fileLogger = replacement;
            (previous as IDisposable)?.Dispose();
        }
    }

    public void Emit(LogEvent logEvent)
    {
        lock (_gate) _fileLogger?.Write(logEvent);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            (_fileLogger as IDisposable)?.Dispose();
            _fileLogger = null;
        }
    }
}
