using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace AtaraxiaAI.Base
{
    public class InMemorySink : ILogEventSink
    {
        readonly ITextFormatter _textFormatter = new MessageTemplateTextFormatter("{Timestamp:HH:mm:ss} {Level:u3} | {Message:lj}{NewLine}{Exception}", CultureInfo.InvariantCulture);

        private readonly ConcurrentQueue<string> _events;
        public ConcurrentQueue<string> Events
        {
            get { return _events; }
        }

        public ObservableCollection<string> Messages { get; set; }

        public InMemorySink()
        {
            _events = new ConcurrentQueue<string>();
            Messages = new ObservableCollection<string>();
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) { throw new ArgumentNullException("LogEvent"); }

            StringWriter renderSpace = new StringWriter();
            _textFormatter.Format(logEvent, renderSpace);
            string formattedLogEvent = renderSpace.ToString();
            Events.Enqueue(formattedLogEvent);

            Messages.Add(formattedLogEvent);
        }
    }
}
