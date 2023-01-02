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
        private List<ComboBoxEnumItem> _captureSourceTypes;

        private ComboBoxEnumItem _selectedCaptureSource;
        public ComboBoxEnumItem SelectedCaptureSource
        {
            get { return _selectedCaptureSource; }
            set
            { 
                SetProperty(ref _selectedCaptureSource, value);

                MainWindowViewModel? mainVM = App.Current?.Services?.GetService<MainWindowViewModel>();
                if (mainVM != null)
                {
                    mainVM.AI.UpdateCaptureSource((CaptureSources)value.Value);
                }
            }
        }

        public SettingsViewModel()
        {
            _captureSourceTypes = Enum.GetValues(typeof(CaptureSources))
                .Cast<CaptureSources>()
                .Select(st => new ComboBoxEnumItem() { Value = (int)st, Text = st.ToString() })
                .ToList();

            _selectedCaptureSource = CaptureSourceTypes
                .Where(cbi => cbi.Value == (int)CaptureSources.Screen)
                .First();
        }
    }
}
