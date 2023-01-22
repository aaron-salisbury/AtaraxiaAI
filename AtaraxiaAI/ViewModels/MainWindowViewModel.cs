using AtaraxiaAI.Business;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Net.Http;
using System.Threading.Tasks;

namespace AtaraxiaAI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public RelayCommand OnVisionClickCommand { get; }
        public RelayCommand OnSoundClickCommand { get; }
        public RelayCommand OnLogsClickCommand { get; }
        public RelayCommand OnSettingsClickCommand { get; }

        public static AI? AI { get; set; }

        [ObservableProperty]
        private bool _activateVision;

        [ObservableProperty]
        private string _visionIcon;

        [ObservableProperty]
        private string _soundIcon;

        [ObservableProperty]
        private bool _showLogs;

        [ObservableProperty]
        private string _logsIcon;

        [ObservableProperty]
        private object? _logsView;

        [ObservableProperty]
        private bool _showSettings;

        [ObservableProperty]
        private string _settingsIcon;

        [ObservableProperty]
        private object? _settingsView;

        [ObservableProperty]
        private object? _visionFeedView;

        public MainWindowViewModel(IHttpClientFactory httpClientFactory)
        {
            AI = new AI(Log.Logger, httpClientFactory);

            _activateVision = false;
            _visionIcon = "EyeOff";
            _soundIcon = "MicOff";
            _logsView = App.Current?.Services?.GetService<LogsViewModel>();
            _showLogs = true;
            _logsIcon = "ClipboardText";
            _settingsView = App.Current?.Services?.GetService<SettingsViewModel>();
            _showSettings = false;
            _settingsIcon = "CogOff";

            VisionFeedViewModel? visionVM = App.Current?.Services?.GetService<VisionFeedViewModel>();
            _visionFeedView = visionVM;

            OnVisionClickCommand = new RelayCommand(() => OnVisionClick());
            OnSoundClickCommand = new RelayCommand(() => OnSoundClick());
            OnLogsClickCommand = new RelayCommand(() => OnLogsClick());
            OnSettingsClickCommand = new RelayCommand(() => OnSettingsClick());

            if (visionVM != null)
            {
                Task.Run(() => { AI.Initiate(updateFrameAction: visionVM.SetVisionFrame).Wait(); });
            }
        }

        public void Shutdown()
        {
            AI?.Shutdown();
        }

        private void OnVisionClick()
        {
            if (AI != null && AI.VisionEngine.IsEngineRunning)
            {
                VisionIcon = "EyeOff";
                AI.VisionEngine.Deactivate();
                ActivateVision = false;
            }
            else
            {
                VisionIcon = "Eye";
                AI?.VisionEngine.Activate();
                ActivateVision = true;
            }
        }

        private void OnSoundClick()
        {
            if (AI != null && AI.SpeechEngine.IsSpeechRecognitionRunning)
            {
                SoundIcon = "MicOff";
                AI.SpeechEngine.DeactivateSpeechRecognition();
            }
            else
            {
                SoundIcon = "Microphone";
                AI?.SpeechEngine.ActivateSpeechRecognition();
            }
        }

        private void OnLogsClick()
        {
            if (ShowLogs)
            {
                LogsIcon = "ClipboardTextOff";
                ShowLogs = false;
            }
            else
            {
                LogsIcon = "ClipboardText";
                ShowLogs = true;
            }
        }

        private void OnSettingsClick()
        {
            if (ShowSettings)
            {
                SettingsIcon = "CogOff";
                ShowSettings = false;
            }
            else
            {
                SettingsIcon = "Cog";
                ShowSettings = true;
            }
        }
    }
}
