using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Speech.Recognition;
using System.Threading;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Integrations.Services;

/// <summary>
/// Windows speech recognition, limited to a grammar of the wake phrase followed by the implemented commands.
/// </summary>
internal sealed class SystemDotSpeechRecognizer : IRecognizer
{
    private readonly CultureInfo _culture;
    private SpeechRecognitionEngine? _recognizer;
    private Action<string>? _speechRecognizedAction;

    internal SystemDotSpeechRecognizer(CultureInfo? culture = null)
    {
        _culture = culture ?? new CultureInfo("en-US");
    }

    public bool IsAvailable()
    {
        return OperatingSystem.IsWindows();
    }

    public Task<bool> PrepareAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        return Task.FromResult(IsAvailable());
    }

    // System.Speech always uses the default input device.
    public void UpdateCaptureSource(SoundCaptureSources source)
    {
    }

    public void Listen(Action<string> speechRecognizedAction)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Stop();

        // The engine is built here rather than in the constructor, so a missing input device fails only when the user
        // starts listening, not when the app starts.
        SpeechRecognitionEngine recognizer = new(_culture);
        try
        {
            recognizer.SetInputToDefaultAudioDevice();
            recognizer.LoadGrammar(GetWakeSkillsGrammar());
            _speechRecognizedAction = speechRecognizedAction;
            recognizer.SpeechRecognized += OnSpeechRecognized;
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }
        catch
        {
            recognizer.Dispose();
            throw;
        }

        _recognizer = recognizer;
    }

    public void Stop()
    {
        if (OperatingSystem.IsWindows() && _recognizer is { } recognizer)
        {
            _recognizer = null;
            recognizer.RecognizeAsyncCancel();
            recognizer.Dispose();
        }
    }

    public void Dispose()
    {
        Stop();
    }

    [SupportedOSPlatform("windows")]
    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        _speechRecognizedAction?.Invoke(e.Result.Text);
    }

    [SupportedOSPlatform("windows")]
    private static Grammar GetWakeSkillsGrammar()
    {
        GrammarBuilder wakeSkills = new(OrchestrationEngine.WAKE_COMMAND);

        wakeSkills.Append(new Choices(
            Enum.GetValues<OrchestrationEngine.SkillMessages>()
                .Select(skill => string.Concat(skill.ToString().Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' '))
                .ToArray()));

        return new Grammar(wakeSkills);
    }
}
