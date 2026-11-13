using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Integrations.Services;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Integrations;

public sealed class IntegrationFactory : IIntegrationFactory, IDisposable
{
    private readonly IntegrationDependencies _dependencies;

    // One instance of each local model provider owns its model download and keeps the loaded model between selections.
    private readonly Lazy<KokoroSynthesizer> _kokoro;
    private readonly Lazy<VoskRecognizer> _vosk;

    /// <param name="modelsDirectory">Where downloaded speech models are kept, each in its own folder.</param>
    public IntegrationFactory(IntegrationDependencies dependencies, IHttpClientFactory httpClientFactory, string modelsDirectory)
    {
        _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelsDirectory);
        _kokoro = new Lazy<KokoroSynthesizer>(() => new KokoroSynthesizer(Path.Combine(modelsDirectory, "Kokoro"), httpClientFactory, dependencies.Logger));
        _vosk = new Lazy<VoskRecognizer>(() => new VoskRecognizer(Path.Combine(modelsDirectory, "Vosk"), httpClientFactory, dependencies.Logger));
    }

    // No configured answer provider is shipped with the app yet.
    public IAnswerProvider? CreateAnswerProvider()
    {
        return null;
    }

    public IInsultService CreateInsultService()
    {
        return new EvilInsultService(_dependencies);
    }

    public IJokeService CreateJokeService(bool dadJoke = false)
    {
        return dadJoke ? new CanHazDadJokeService(_dependencies) : new Sv443JokeService(_dependencies);
    }

    public IObjectDetector CreateObjectDetector()
    {
        return new YoloObjectDetector(_dependencies);
    }

    public IOpticalCharacterRecognizer CreateOpticalCharacterRecognizer()
    {
        return new TesseractOCR(_dependencies);
    }

    public IRecognizer CreateRecognizer(SpeechRecognizers recognizer, CultureInfo culture)
    {
        return recognizer switch
        {
            SpeechRecognizers.Vosk => VoskRecognizer.Supports(culture)
                ? _vosk.Value
                : throw new NotSupportedException($"Vosk does not recognize {culture.Name}."),
            SpeechRecognizers.SystemDotSpeech => new SystemDotSpeechRecognizer(culture),
            _ => throw new ArgumentOutOfRangeException(nameof(recognizer))
        };
    }

    public IStreamingAvailabilityService CreateStreamingAvailabilityService()
    {
        return new WatchModeStreamingAvailabilityService(_dependencies);
    }

    public ISynthesizer CreateSynthesizer(SpeechSynthesizers synthesizer, CultureInfo culture)
    {
        return synthesizer switch
        {
            SpeechSynthesizers.Kokoro => KokoroSynthesizer.Supports(culture)
                ? _kokoro.Value
                : throw new NotSupportedException($"Kokoro does not speak {culture.Name}."),
            SpeechSynthesizers.SystemDotSpeech => new SystemDotSpeechSynthesizer(culture, _dependencies),
            _ => throw new ArgumentOutOfRangeException(nameof(synthesizer))
        };
    }

    public void Dispose()
    {
        if (_kokoro.IsValueCreated)
        {
            _kokoro.Value.Dispose();
        }

        if (_vosk.IsValueCreated)
        {
            _vosk.Value.Dispose();
        }
    }
}
