using System;
using System.Threading;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Business.Services;

/// <summary>
/// Turns captured audio into text. The integration factory owns each recognizer's lifetime; callers start and stop
/// listening with <see cref="Listen"/> and <see cref="Stop"/>.
/// </summary>
public interface IRecognizer : IDisposable
{
    bool IsAvailable();

    /// <summary>
    /// Readies the recognizer before first use, such as downloading and loading a local model.
    /// </summary>
    /// <param name="progress">Receives download progress from 0 to 100 when the recognizer must download something.</param>
    /// <returns>Whether the recognizer is ready; reasons it is not are logged.</returns>
    Task<bool> PrepareAsync(IProgress<double>? progress, CancellationToken cancellationToken);

    /// <summary>Starts listening, calling <paramref name="speechRecognizedAction"/> with each phrase heard.</summary>
    void Listen(Action<string> speechRecognizedAction);

    /// <summary>Stops listening and releases the audio device. <see cref="Listen"/> may be called again.</summary>
    void Stop();

    /// <summary>Takes effect the next time listening starts.</summary>
    void UpdateCaptureSource(SoundCaptureSources source);

    //TODO: Potential future implementations:
    //    https://fosspost.org/open-source-speech-recognition/
    //    Syn.Speech  |  Cross-platform but currently relies on .Net Framework. https://developer.syn.co.in/tutorial/speech/recognition-without-grammar.html
    //    DeepSpeech  |  Very complicated. The DeepSpeach libraries would probably work, but the pbmm model exceeds github limit and DeepSpeech-TFLite relies on .Net Framework https://deepspeech.readthedocs.io/en/r0.9/DotNet-API.html
    //    Coqui STT   |  https://github.com/coqui-ai/STT    https://stt.readthedocs.io/en/latest/DotNet-API.html    https://github.com/coqui-ai/STT-models/releases
}
