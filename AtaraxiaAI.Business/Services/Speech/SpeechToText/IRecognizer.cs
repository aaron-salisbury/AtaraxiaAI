using System;

namespace AtaraxiaAI.Business.Services
{
    internal interface IRecognizer
    {
        bool IsAvailable();

        void Shutdown();

        void Listen(Action<string> speechRecognizedAction);
    }
}
