using AtaraxiaAI.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private List<ComboBoxEnumItem> _visionCaptureSourceTypes;

        [ObservableProperty]
        private List<ComboBoxEnumItem> _soundCaptureSourceTypes;

        private ComboBoxEnumItem _selectedVisionCaptureSource;
        public ComboBoxEnumItem SelectedVisionCaptureSource
        {
            get { return _selectedVisionCaptureSource; }
            set
            { 
                SetProperty(ref _selectedVisionCaptureSource, value);
                MainWindowViewModel.AI?.VisionEngine.UpdateCaptureSource((VisionCaptureSources)value.Value);
            }
        }

        private ComboBoxEnumItem _selectedSoundCaptureSource;
        public ComboBoxEnumItem SelectedSoundCaptureSource
        {
            get { return _selectedSoundCaptureSource; }
            set
            {
                SetProperty(ref _selectedSoundCaptureSource, value);
                MainWindowViewModel.AI?.SpeechEngine.UpdateCaptureSource((SoundCaptureSources)value.Value);
            }
        }

        private string _userStorageDirectory;
        public string UserStorageDirectory
        {
            get { return _userStorageDirectory; }
            set
            {
                SetProperty(ref _userStorageDirectory, value);
                MainWindowViewModel.AI?.UpdateUserStorageDirectory(_userStorageDirectory);
            }
        }

        public SettingsViewModel()
        {
            _userStorageDirectory = MainWindowViewModel.AI?.GetUserStorageDirectory() ?? string.Empty;

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
}
