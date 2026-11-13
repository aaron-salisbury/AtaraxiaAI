using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Presentation.Desktop.Settings;
using RunnethOverStudio.AppToolkit.Modules.Access;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Tests;

/// <summary>Implements an interface by routing every call to one function, for interfaces a test only touches lightly.</summary>
public class Proxy<T> : DispatchProxy where T : class
{
    private Func<MethodInfo, object?[]?, object?> _invoke = null!;

    public static T Create(Func<MethodInfo, object?[]?, object?> invoke)
    {
        T instance = DispatchProxy.Create<T, Proxy<T>>();
        ((Proxy<T>)(object)instance)._invoke = invoke;
        return instance;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        return _invoke(targetMethod!, args);
    }
}

internal static class TestDependencies
{
    internal static ILogger Logger { get; } = new LoggerConfiguration().CreateLogger();

    internal static IHttpRequester NoHttp { get; } =
        Proxy<IHttpRequester>.Create((method, _) => throw new InvalidOperationException($"Unexpected HTTP call: {method.Name}"));

    internal static IntegrationDependencies Create(IHttpRequester? http = null)
    {
        return new IntegrationDependencies(http ?? NoHttp, Logger);
    }

    internal static IAudioPlayer SilentPlayer { get; } = Proxy<IAudioPlayer>.Create((_, _) => Task.CompletedTask);

    /// <summary>Waits for a condition that asynchronous work makes true, failing after a few seconds.</summary>
    internal static async Task WaitUntilAsync(Func<bool> condition)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }
}

internal sealed class MemoryDesktopSettings : IDesktopSettings
{
    public SpeechSettings Speech { get; set; } = new();
    public int Saves { get; private set; }

    public SpeechSettings GetSpeechSettings()
    {
        return Speech;
    }

    public void SetSpeechSettings(SpeechSettings settings)
    {
        Speech = settings;
        Saves++;
    }
}

internal sealed class FakeSynthesizer : ISynthesizer
{
    public Func<IProgress<double>?, CancellationToken, Task<bool>> Prepare { get; set; } = (_, _) => Task.FromResult(true);
    public Func<string, CancellationToken, Task<byte[]>> Synthesize { get; set; } = (_, _) => Task.FromResult(new byte[] { 1, 2, 3 });
    public IReadOnlyList<SpeechVoice> Voices { get; set; } = [];
    public bool Available { get; set; } = true;
    public int PrepareCalls { get; private set; }
    public List<string> Spoken { get; } = [];
    public List<string?> SpokenVoices { get; } = [];

    public bool IsAvailable()
    {
        return Available;
    }

    public Task<bool> PrepareAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        PrepareCalls++;
        return Prepare(progress, cancellationToken);
    }

    public Task<byte[]> SynthesizeAsync(string message, string? voiceId, CancellationToken cancellationToken)
    {
        Spoken.Add(message);
        SpokenVoices.Add(voiceId);
        return Synthesize(message, cancellationToken);
    }
}

internal sealed class FakeRecognizer : IRecognizer
{
    private Action<string>? _callback;
    public int PauseCalls { get; private set; }

    public bool IsAvailable()
    {
        return true;
    }

    public void Listen(Action<string> speechRecognizedAction)
    {
        _callback = speechRecognizedAction;
    }

    public void Fire(string text)
    {
        _callback!(text);
    }

    public void UpdateCaptureSource(SoundCaptureSources source)
    {
    }

    public void Pause()
    {
        PauseCalls++;
    }

    public void Unpause()
    {
    }

    public void Dispose()
    {
    }
}

internal sealed class FakeIntegrationFactory : IIntegrationFactory
{
    public Dictionary<SpeechSynthesizers, FakeSynthesizer> Synthesizers { get; } =
        Enum.GetValues<SpeechSynthesizers>().ToDictionary(kind => kind, _ => new FakeSynthesizer());

    public FakeRecognizer Recognizer { get; } = new();
    public IJokeService? JokeService { get; set; }

    public IAnswerProvider? CreateAnswerProvider()
    {
        return null;
    }

    public IInsultService CreateInsultService()
    {
        throw new NotSupportedException();
    }

    public IJokeService CreateJokeService(bool dadJoke = false)
    {
        return JokeService ?? throw new NotSupportedException();
    }

    public IObjectDetector CreateObjectDetector()
    {
        throw new NotSupportedException();
    }

    public IOpticalCharacterRecognizer CreateOpticalCharacterRecognizer()
    {
        throw new NotSupportedException();
    }

    public IRecognizer CreateRecognizer(System.Globalization.CultureInfo culture)
    {
        return Recognizer;
    }

    public IStreamingAvailabilityService CreateStreamingAvailabilityService()
    {
        return null!;
    }

    public ISynthesizer CreateSynthesizer(SpeechSynthesizers synthesizer, System.Globalization.CultureInfo culture)
    {
        return Synthesizers[synthesizer];
    }
}
