using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Integrations.Services;
using System;
using System.Globalization;
using System.Net.Http;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Integrations;

public sealed class IntegrationFactory : IIntegrationFactory, IDisposable
{
    private readonly IntegrationDependencies _dependencies;

    // One Kokoro instance owns the model download and keeps the loaded model between selections.
    private readonly Lazy<KokoroSynthesizer> _kokoro;

    /// <param name="kokoroDirectory">Where the downloaded Kokoro speech model is kept.</param>
    public IntegrationFactory(IntegrationDependencies dependencies, IHttpClientFactory httpClientFactory, string kokoroDirectory)
    {
        _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(kokoroDirectory);
        _kokoro = new Lazy<KokoroSynthesizer>(() => new KokoroSynthesizer(kokoroDirectory, httpClientFactory, dependencies.Logger));
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

    public IRecognizer CreateRecognizer(CultureInfo culture)
    {
        return new SystemDotSpeechRecognizer(culture);
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
    }
}
