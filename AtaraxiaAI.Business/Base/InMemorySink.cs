using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace AtaraxiaAI.Business.Base
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

        //private string _messages;
        //public string Messages
        //{
        //    get { return _messages; }
        //    set
        //    {
        //        _messages = value;
        //        RaisePropertyChanged("Messages");

        //        //if (value == null)
        //        //{
        //        //    _messages = value;
        //        //    RaisePropertyChanged("Messages");
        //        //}
        //        //else if (!string.Equals(value, _messages))
        //        //{
        //        //    // Create typing effect.
        //        //    string newMessage = _messages != null ? value.Substring(_messages.Length) : value;
        //        //    foreach (char letter in newMessage)
        //        //    {
        //        //        System.Threading.Thread.Sleep(30);
        //        //        _messages += letter;
        //        //        RaisePropertyChanged("Messages");
        //        //    }
        //        //}
        //    }
        //}

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
