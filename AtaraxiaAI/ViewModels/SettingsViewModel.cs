using AtaraxiaAI.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
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

                MainWindowViewModel? mainVM = App.Current?.Services?.GetService<MainWindowViewModel>();
                if (mainVM != null)
                {
                    mainVM.AI.VisionEngine.UpdateCaptureSource((VisionCaptureSources)value.Value);
                }
            }
        }

        private ComboBoxEnumItem _selectedSoundCaptureSource;
        public ComboBoxEnumItem SelectedSoundCaptureSource
        {
            get { return _selectedSoundCaptureSource; }
            set
            {
                SetProperty(ref _selectedSoundCaptureSource, value);

                MainWindowViewModel? mainVM = App.Current?.Services?.GetService<MainWindowViewModel>();
                if (mainVM != null)
                {
                    mainVM.AI.UpdateSoundCaptureSource((SoundCaptureSources)value.Value);
                }
            }
        }

        public SettingsViewModel()
        {
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
