using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Presentation.Desktop.Base;
using AtaraxiaAI.Presentation.Desktop.Models;
using AtaraxiaAI.Presentation.Desktop.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Presentation.Desktop.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty]
    private List<ComboBoxEnumItem> _visionCaptureSourceTypes;

    [ObservableProperty]
    private List<ComboBoxEnumItem> _soundCaptureSourceTypes;

    [ObservableProperty]
    private List<ComboBoxEnumItem> _recognizerSourceTypes;

    [ObservableProperty]
    private List<ComboBoxEnumItem> _speechSourceTypes;

    [ObservableProperty]
    private List<ComboBoxEnumItem> _voiceGenderTypes;

    /// <summary>The speech source's voices of the chosen gender.</summary>
    [ObservableProperty]
    private List<SpeechVoice> _voices = [];

    /// <summary>Whether the speech source in use offers a choice of voices, which reveals the voice controls.</summary>
    [ObservableProperty]
    private bool _hasVoiceChoices;

    private readonly AI _ai;
    private readonly IDesktopSettings _desktopSettings;
    private RecognitionSettings _recognitionSettings;
    private SpeechSettings _speechSettings;
    private CancellationTokenSource? _voiceSample;
    private bool _showingVoices;

    /// <summary>Progress while a chosen recognizer source is prepared, such as Vosk's one-time model download.</summary>
    public SourcePreparation RecognizerPreparation { get; } = new();

    /// <summary>Progress while a chosen speech source is prepared, such as Kokoro's one-time model download.</summary>
    public SourcePreparation SpeechPreparation { get; } = new();

    private ComboBoxEnumItem _selectedVisionCaptureSource;
    public ComboBoxEnumItem SelectedVisionCaptureSource
    {
        get => _selectedVisionCaptureSource;
        set
        {
            SetProperty(ref _selectedVisionCaptureSource, value);
            if (value is not null)
            {
                _ai.VisionEngine?.UpdateCaptureSource((VisionCaptureSources)value.Value);
            }
        }
    }

    private ComboBoxEnumItem _selectedSoundCaptureSource;
    public ComboBoxEnumItem SelectedSoundCaptureSource
    {
        get => _selectedSoundCaptureSource;
        set
        {
            SetProperty(ref _selectedSoundCaptureSource, value);
            if (value is not null)
            {
                _ai.SpeechEngine?.UpdateCaptureSource((SoundCaptureSources)value.Value);
            }
        }
    }

    private ComboBoxEnumItem _selectedRecognizerSource;
    public ComboBoxEnumItem SelectedRecognizerSource
    {
        get => _selectedRecognizerSource;
        set
        {
            if (SetProperty(ref _selectedRecognizerSource, value) && value is not null)
            {
                _ = SelectRecognizerSourceAsync((SpeechRecognizers)value.Value);
            }
        }
    }

    private ComboBoxEnumItem _selectedSpeechSource;
    public ComboBoxEnumItem SelectedSpeechSource
    {
        get => _selectedSpeechSource;
        set
        {
            if (SetProperty(ref _selectedSpeechSource, value) && value is not null)
            {
                _ = SelectSpeechSourceAsync((SpeechSynthesizers)value.Value);
            }
        }
    }

    private ComboBoxEnumItem _selectedVoiceGender;
    public ComboBoxEnumItem SelectedVoiceGender
    {
        get => _selectedVoiceGender;
        set
        {
            if (SetProperty(ref _selectedVoiceGender, value) && value is not null)
            {
                SaveSpeechSettings(_speechSettings with { VoiceGender = (VoiceGenderFilter)value.Value });
                ShowVoices();
            }
        }
    }

    private SpeechVoice? _selectedVoice;
    /// <summary>The chosen voice. Choosing one stores it and plays a short sample of it.</summary>
    public SpeechVoice? SelectedVoice
    {
        get => _selectedVoice;
        set
        {
            // The list clears its selection when the gender filter hides the chosen voice; that is not a new choice.
            if (!SetProperty(ref _selectedVoice, value) || value is null || _showingVoices || _ai.SpeechEngine is not { } engine)
            {
                return;
            }

            engine.VoiceId = value.Id;
            SaveSpeechSettings(_speechSettings with { VoiceId = value.Id });
            _ = PlayVoiceSampleAsync(engine, value.Id);
        }
    }

    public SettingsViewModel(AI ai, IDesktopSettings desktopSettings)
    {
        _ai = ai;
        _desktopSettings = desktopSettings ?? throw new ArgumentNullException(nameof(desktopSettings));
        _recognitionSettings = desktopSettings.GetRecognitionSettings();
        _speechSettings = desktopSettings.GetSpeechSettings();

        _visionCaptureSourceTypes = [.. Enum.GetValues<VisionCaptureSources>().Select(ComboBoxEnumItem.From)];

        _soundCaptureSourceTypes = [.. Enum.GetValues<SoundCaptureSources>().Select(ComboBoxEnumItem.From)];

        _recognizerSourceTypes = [.. SpeechEngine.SelectableRecognizers.Select(ComboBoxEnumItem.From)];

        _speechSourceTypes = [.. SpeechEngine.SelectableSynthesizers.Select(ComboBoxEnumItem.From)];

        _voiceGenderTypes = [.. Enum.GetValues<VoiceGenderFilter>().Select(ComboBoxEnumItem.From)];

        //TODO: Single source setting these.
        _selectedVisionCaptureSource = VisionCaptureSourceTypes
            .Where(cbi => cbi.Value == (int)VisionCaptureSources.Screen)
            .First();

        _selectedSoundCaptureSource = SoundCaptureSourceTypes
            .Where(cbi => cbi.Value == (int)SoundCaptureSources.SoundCard)
            .First();

        _selectedRecognizerSource = RecognizerSourceTypes.FirstOrDefault(cbi => cbi.Value == (int)_recognitionSettings.Source)
            ?? RecognizerSourceTypes.First(cbi => cbi.Value == (int)SpeechEngine.DEFAULT_RECOGNIZER);

        _selectedSpeechSource = SpeechSourceTypes.FirstOrDefault(cbi => cbi.Value == (int)_speechSettings.Source)
            ?? SpeechSourceTypes.First(cbi => cbi.Value == (int)SpeechEngine.DEFAULT_SYNTHESIZER);

        _selectedVoiceGender = VoiceGenderTypes.FirstOrDefault(cbi => cbi.Value == (int)_speechSettings.VoiceGender)
            ?? VoiceGenderTypes.First(cbi => cbi.Value == (int)VoiceGenderFilter.Both);
    }

    /// <summary>
    /// Applies the stored recognizer and speech choices once the AI has initialized, which may resume downloading a
    /// stored source's model.
    /// </summary>
    public async Task ApplyStoredSettingsAsync()
    {
        if (_ai.SpeechEngine is not { } engine)
        {
            return;
        }

        engine.VoiceId = _speechSettings.VoiceId;

        // Read both choices before starting either, since a selection that completes shows the source actually in use.
        SpeechRecognizers storedRecognizer = (SpeechRecognizers)_selectedRecognizerSource.Value;
        SpeechSynthesizers storedSpeech = (SpeechSynthesizers)_selectedSpeechSource.Value;

        Task recognizer = engine.ActiveRecognizer == storedRecognizer
            ? Task.CompletedTask
            : SelectRecognizerSourceAsync(storedRecognizer);

        Task speech;
        if (engine.ActiveSynthesizer == storedSpeech)
        {
            ShowVoices();
            speech = Task.CompletedTask;
        }
        else
        {
            speech = SelectSpeechSourceAsync(storedSpeech);
        }

        await Task.WhenAll(recognizer, speech);
    }

    /// <summary>
    /// Shows the recognizer and speech sources actually in use, which differ from the selections when a source could not
    /// be prepared or failed while speaking.
    /// </summary>
    public void RefreshSources()
    {
        RefreshRecognizerSource();
        RefreshSpeechSource();
    }

    private void RefreshRecognizerSource()
    {
        if (!RecognizerPreparation.IsPreparing
            && _ai.SpeechEngine is { } engine
            && RecognizerSourceTypes.FirstOrDefault(cbi => cbi.Value == (int)engine.ActiveRecognizer) is { } recognizer
            && !ReferenceEquals(recognizer, _selectedRecognizerSource))
        {
            // Set the field directly so showing the actual source does not select it again.
            _selectedRecognizerSource = recognizer;
            OnPropertyChanged(nameof(SelectedRecognizerSource));
        }
    }

    private void RefreshSpeechSource()
    {
        if (!SpeechPreparation.IsPreparing
            && _ai.SpeechEngine?.ActiveSynthesizer is { } active
            && SpeechSourceTypes.FirstOrDefault(cbi => cbi.Value == (int)active) is { } speech
            && !ReferenceEquals(speech, _selectedSpeechSource))
        {
            _selectedSpeechSource = speech;
            OnPropertyChanged(nameof(SelectedSpeechSource));
        }

        ShowVoices();
    }

    /// <summary>
    /// Switches recognition to the chosen source, showing download progress when it needs a model. Choosing another
    /// source meanwhile cancels the unfinished download, which resumes the next time the source is chosen.
    /// </summary>
    private async Task SelectRecognizerSourceAsync(SpeechRecognizers requested)
    {
        if (_ai.SpeechEngine is not { } engine)
        {
            return;
        }

        bool? selected = await RecognizerPreparation.RunAsync(
            DisplayName(requested),
            (progress, cancellationToken) => engine.SelectRecognizerAsync(requested, progress, cancellationToken));

        if (selected is null)
        {
            return;
        }

        if (selected.Value)
        {
            _recognitionSettings = _recognitionSettings with { Source = requested };
            Save(() => _desktopSettings.SetRecognitionSettings(_recognitionSettings));
        }

        RefreshRecognizerSource();
    }

    /// <summary>
    /// Switches speech to the chosen source, showing download progress when it needs a model. Choosing another source
    /// meanwhile cancels the unfinished download, which resumes the next time the source is chosen.
    /// </summary>
    private async Task SelectSpeechSourceAsync(SpeechSynthesizers requested)
    {
        if (_ai.SpeechEngine is not { } engine)
        {
            return;
        }

        Task<bool?> preparing = SpeechPreparation.RunAsync(
            DisplayName(requested),
            (progress, cancellationToken) => engine.SelectSynthesizerAsync(requested, progress, cancellationToken));

        // Preparation has started, so this hides the previous source's voices until the new source is ready.
        ShowVoices();
        bool? selected = await preparing;

        if (selected is null)
        {
            return;
        }

        if (selected.Value)
        {
            SaveSpeechSettings(_speechSettings with { Source = requested });
        }

        RefreshSpeechSource();
    }

    /// <summary>
    /// Lists the voices of the source in use that match the gender filter, highlighting the chosen voice, or the
    /// default voice when none was chosen.
    /// </summary>
    private void ShowVoices()
    {
        IReadOnlyList<SpeechVoice> available = SpeechPreparation.IsPreparing ? [] : _ai.SpeechEngine?.ActiveVoices ?? [];
        VoiceGenderFilter filter = (VoiceGenderFilter)_selectedVoiceGender.Value;
        string? chosen = available.Any(voice => voice.Id == _speechSettings.VoiceId) ? _speechSettings.VoiceId : available.FirstOrDefault()?.Id;

        _showingVoices = true;
        try
        {
            HasVoiceChoices = available.Count > 0;
            Voices = [.. available
                .Where(voice => filter == VoiceGenderFilter.Both
                    || (filter == VoiceGenderFilter.Male) == (voice.Gender == VoiceGender.Male))];
            SelectedVoice = Voices.FirstOrDefault(voice => voice.Id == chosen);
        }
        finally
        {
            _showingVoices = false;
        }
    }

    private async Task PlayVoiceSampleAsync(SpeechEngine engine, string voiceId)
    {
        // A newer choice interrupts the sample still playing.
        _voiceSample?.Cancel();
        using CancellationTokenSource sample = new();
        _voiceSample = sample;

        try
        {
            await engine.PlayVoiceSampleAsync(voiceId, sample.Token);
        }
        finally
        {
            if (ReferenceEquals(_voiceSample, sample))
            {
                _voiceSample = null;
            }
        }
    }

    private void SaveSpeechSettings(SpeechSettings settings)
    {
        _speechSettings = settings;
        Save(() => _desktopSettings.SetSpeechSettings(settings));
    }

    private static void Save(Action save)
    {
        try
        {
            save();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Log.Warning(error, "Could not save the desktop settings.");
        }
    }

    private static string DisplayName<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        return ComboBoxEnumItem.From(value).Text ?? value.ToString();
    }
}
