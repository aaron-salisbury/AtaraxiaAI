using Emgu.CV.Saliency;
using Serilog;
using System.Linq.Expressions;

namespace AtaraxiaAI.Business.Base
{
    public class InMemoryLogger
    {
        public const string NEW_LINE_PREFIX = "               ";

        public ILogger Logger { get; set; }
        public InMemorySink InMemorySink { get; set; }

        public InMemoryLogger()
        {
            InMemorySink = new InMemorySink();

            Logger = new LoggerConfiguration()
                .WriteTo.Sink(InMemorySink)
                .CreateLogger();
        }
    }
}
