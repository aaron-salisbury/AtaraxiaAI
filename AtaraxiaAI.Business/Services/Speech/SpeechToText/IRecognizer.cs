using System;

namespace AtaraxiaAI.Business.Services
{
    internal interface IRecognizer : IDisposable
    {
        bool IsAvailable();

        void Listen(Action<string> speechRecognizedAction);

        //TODO: Potential future implementations:
        //    https://fosspost.org/open-source-speech-recognition/
        //    Syn.Speech  |  Cross-platform but currently relies on .Net Framework. https://developer.syn.co.in/tutorial/speech/recognition-without-grammar.html
        //    Vosk        |  So far it looks like it requires a wav file to be sent and can't just always listen. https://alphacephei.com/vosk/
        //    DeepSpeech  |  Very complicated. The DeepSpeach libraries would probably work, but the pbmm model exceeds github limit and DeepSpeech-TFLite relies on .Net Framework https://deepspeech.readthedocs.io/en/r0.9/DotNet-Examples.html
    }
}
