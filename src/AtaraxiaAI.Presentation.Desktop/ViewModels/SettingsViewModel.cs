using AtaraxiaAI.Business;
using AtaraxiaAI.Presentation.Desktop.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Presentation.Desktop.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty]
    private List<ComboBoxEnumItem> _visionCaptureSourceTypes;

    [ObservableProperty]
    private List<ComboBoxEnumItem> _soundCaptureSourceTypes;

    private readonly AI _ai;

    private ComboBoxEnumItem _selectedVisionCaptureSource;
    public ComboBoxEnumItem SelectedVisionCaptureSource
    {
        get => _selectedVisionCaptureSource;
        set
        {
            SetProperty(ref _selectedVisionCaptureSource, value);
            if (value is not null)
                _ai.VisionEngine?.UpdateCaptureSource((VisionCaptureSources)value.Value);
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
                _ai.SpeechEngine?.UpdateCaptureSource((SoundCaptureSources)value.Value);
        }
    }

    public SettingsViewModel(AI ai)
    {
        _ai = ai;

        _visionCaptureSourceTypes = Enum.GetValues(typeof(VisionCaptureSources))
            .Cast<VisionCaptureSources>()
            .Select(cs => new ComboBoxEnumItem() { Value = (int)cs, Text = cs.ToString() })
            .ToList();

        _soundCaptureSourceTypes = Enum.GetValues(typeof(SoundCaptureSources))
            .Cast<SoundCaptureSources>()
            .Select(cs => new ComboBoxEnumItem() { Value = (int)cs, Text = cs.ToString() })
            .ToList();

        //TODO: Single source setting these.
        _selectedVisionCaptureSource = VisionCaptureSourceTypes
            .Where(cbi => cbi.Value == (int)VisionCaptureSources.Screen)
            .First();

        _selectedSoundCaptureSource = SoundCaptureSourceTypes
            .Where(cbi => cbi.Value == (int)SoundCaptureSources.SoundCard)
            .First();
    }
}
