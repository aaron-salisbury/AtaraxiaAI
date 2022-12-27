using System;

namespace AtaraxiaAI.Business.Services
{
    // https://www.rev.com/blog/resources/the-5-best-open-source-speech-recognition-engines-apis
    public interface ISpeechEngine
    {
        void Listen(Action<string> speechRecognizedAction);

        void Speek(string message);
    }
}
