using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Presentation.Desktop.Base;
using AtaraxiaAI.Presentation.Desktop.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using Serilog;
using System;
using System.Collections.Generic;
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
    private List<ComboBoxEnumItem> _speechSourceTypes;

    [ObservableProperty]
    private List<ComboBoxEnumItem> _voiceGenderTypes;

    /// <summary>The speech source's voices of the chosen gender.</summary>
    [ObservableProperty]
    private List<SpeechVoice> _voices = [];

    /// <summary>Whether the speech source in use offers a choice of voices, which reveals the voice controls.</summary>
    [ObservableProperty]
    private bool _hasVoiceChoices;

    /// <summary>Whether a speech source is being prepared, which may include downloading its model.</summary>
    [ObservableProperty]
    private bool _isPreparingSpeech;

    /// <summary>Whether <see cref="SpeechProgress"/> reports a download; otherwise preparation has no known length.</summary>
    [ObservableProperty]
    private bool _isSpeechProgressKnown;

    [ObservableProperty]
    private double _speechProgress;

    [ObservableProperty]
    private string? _speechStatus;

    private readonly AI _ai;
    private readonly IDesktopSettings _desktopSettings;
    private SpeechSettings _speechSettings;
    private CancellationTokenSource? _speechSelection;
    private CancellationTokenSource? _voiceSample;
    private bool _showingVoices;

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
        _speechSettings = desktopSettings.GetSpeechSettings();

        _visionCaptureSourceTypes = [.. Enum.GetValues<VisionCaptureSources>().Select(ComboBoxEnumItem.From)];

        _soundCaptureSourceTypes = [.. Enum.GetValues<SoundCaptureSources>().Select(ComboBoxEnumItem.From)];

        _speechSourceTypes = [.. SpeechEngine.SelectableSynthesizers.Select(ComboBoxEnumItem.From)];

        _voiceGenderTypes = [.. Enum.GetValues<VoiceGenderFilter>().Select(ComboBoxEnumItem.From)];

        //TODO: Single source setting these.
        _selectedVisionCaptureSource = VisionCaptureSourceTypes
            .Where(cbi => cbi.Value == (int)VisionCaptureSources.Screen)
            .First();

        _selectedSoundCaptureSource = SoundCaptureSourceTypes
            .Where(cbi => cbi.Value == (int)SoundCaptureSources.SoundCard)
            .First();

        _selectedSpeechSource = SpeechSourceTypes.FirstOrDefault(cbi => cbi.Value == (int)_speechSettings.Source)
            ?? SpeechSourceTypes.First(cbi => cbi.Value == (int)SpeechEngine.DEFAULT_SYNTHESIZER);

        _selectedVoiceGender = VoiceGenderTypes.FirstOrDefault(cbi => cbi.Value == (int)_speechSettings.VoiceGender)
            ?? VoiceGenderTypes.First(cbi => cbi.Value == (int)VoiceGenderFilter.Both);
    }

    /// <summary>
    /// Applies the stored speech choices once the AI has initialized, which may resume downloading a stored source.
    /// </summary>
    public async Task ApplyStoredSpeechSettingsAsync()
    {
        if (_ai.SpeechEngine is not { } engine)
        {
            return;
        }

        engine.VoiceId = _speechSettings.VoiceId;
        if (engine.ActiveSynthesizer == (SpeechSynthesizers)_selectedSpeechSource.Value)
        {
            ShowVoices();
            return;
        }

        await SelectSpeechSourceAsync((SpeechSynthesizers)_selectedSpeechSource.Value);
    }

    /// <summary>
    /// Shows the speech source actually in use, which differs from the selection when a source could not be prepared or
    /// failed while speaking.
    /// </summary>
    public void RefreshSpeechSource()
    {
        if (IsPreparingSpeech || _ai.SpeechEngine?.ActiveSynthesizer is not { } active)
        {
            return;
        }

        if (SpeechSourceTypes.FirstOrDefault(cbi => cbi.Value == (int)active) is { } item && !ReferenceEquals(item, _selectedSpeechSource))
        {
            // Set the field directly so showing the actual source does not select it again.
            _selectedSpeechSource = item;
            OnPropertyChanged(nameof(SelectedSpeechSource));
        }

        ShowVoices();
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

        string name = ComboBoxEnumItem.From(requested).Text ?? requested.ToString();
        _speechSelection?.Cancel();
        CancellationTokenSource selection = new();
        _speechSelection = selection;

        IsPreparingSpeech = true;
        IsSpeechProgressKnown = false;
        SpeechProgress = 0;
        SpeechStatus = $"Preparing {name}…";
        ShowVoices();

        Progress<double> progress = new(percent =>
        {
            if (ReferenceEquals(_speechSelection, selection))
            {
                IsSpeechProgressKnown = true;
                SpeechProgress = percent;
                SpeechStatus = $"Downloading {name}… {percent:0}%";
            }
        });

        try
        {
            bool selected = await engine.SelectSynthesizerAsync(requested, progress, selection.Token);
            if (ReferenceEquals(_speechSelection, selection))
            {
                SpeechStatus = selected ? null : $"{name} is unavailable. See the logs for details.";
                if (selected)
                {
                    SaveSpeechSettings(_speechSettings with { Source = requested });
                }
            }
        }
        catch (OperationCanceledException) when (selection.IsCancellationRequested)
        {
            // Another source was chosen; its selection reports the outcome.
        }
        catch (Exception error)
        {
            Log.Error(error, "Failed to change the speech source to {Provider}.", requested);
            if (ReferenceEquals(_speechSelection, selection))
            {
                SpeechStatus = $"Could not change the speech source: {error.Message}";
            }
        }
        finally
        {
            if (ReferenceEquals(_speechSelection, selection))
            {
                _speechSelection = null;
                IsPreparingSpeech = false;
                RefreshSpeechSource();
            }

            selection.Dispose();
        }
    }

    /// <summary>
    /// Lists the voices of the source in use that match the gender filter, highlighting the chosen voice, or the
    /// default voice when none was chosen.
    /// </summary>
    private void ShowVoices()
    {
        IReadOnlyList<SpeechVoice> available = IsPreparingSpeech ? [] : _ai.SpeechEngine?.ActiveVoices ?? [];
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

        try
        {
            _desktopSettings.SetSpeechSettings(settings);
        }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException)
        {
            Log.Warning(error, "Could not save the speech settings.");
        }
    }
}
