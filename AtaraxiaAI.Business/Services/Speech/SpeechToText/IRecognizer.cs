using System;

namespace AtaraxiaAI.Business.Services
{
    public interface IRecognizer
    {
        bool IsAvailable();

        void Listen(Action<string> speechRecognizedAction);
    }
}
