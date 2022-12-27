using Serilog;

namespace AtaraxiaAI.Business.Base
{
    public class InMemoryLogger
    {
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
