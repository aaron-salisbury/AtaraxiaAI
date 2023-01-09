using AtaraxiaAI.Business;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
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
        private IBitmap? _visionFrame;

        [ObservableProperty]
        private bool _showVisionFeed;

        [ObservableProperty]
        private MaterialIconKind _visionIcon;

        [ObservableProperty]
        private MaterialIconKind _soundIcon;

        [ObservableProperty]
        private bool _showLogs;

        [ObservableProperty]
        private MaterialIconKind _logsIcon;

        [ObservableProperty]
        private object? _logsView;

        [ObservableProperty]
        private bool _showSettings;

        [ObservableProperty]
        private MaterialIconKind _settingsIcon;

        [ObservableProperty]
        private object? _settingsView;

        public MainWindowViewModel(IHttpClientFactory httpClientFactory)
        {
            AI = new AI(httpClientFactory);

            _showVisionFeed = false;
            _visionIcon = MaterialIconKind.EyeOff;
            _soundIcon = MaterialIconKind.MicOff;
            _logsView = App.Current?.Services?.GetService<LogsViewModel>();
            _showLogs = true;
            _logsIcon = MaterialIconKind.ClipboardText;
            _settingsView = App.Current?.Services?.GetService<SettingsViewModel>();
            _showSettings = false;
            _settingsIcon = MaterialIconKind.CogOff;

            OnVisionClickCommand = new RelayCommand(() => OnVisionClick());
            OnSoundClickCommand = new RelayCommand(() => OnSoundClick());
            OnLogsClickCommand = new RelayCommand(() => OnLogsClick());
            OnSettingsClickCommand = new RelayCommand(() => OnSettingsClick());

            Task.Run(() => { AI.Initiate(updateFrameAction: SetVisionFrame).Wait(); });
        }

        public void Shutdown()
        {
            AI?.Shutdown();
        }

        private void OnVisionClick()
        {
            if (AI != null && AI.VisionEngine.IsEngineRunning)
            {
                VisionIcon = MaterialIconKind.EyeOff;
                AI.VisionEngine.Deactivate();
                ShowVisionFeed = false;
            }
            else
            {
                VisionIcon = MaterialIconKind.Eye;
                AI?.VisionEngine.Activate();
                ShowVisionFeed = true;
            }
        }

        private void OnSoundClick()
        {
            if (AI != null && AI.SpeechEngine.IsSpeechRecognitionRunning)
            {
                SoundIcon = MaterialIconKind.MicOff;
                AI.SpeechEngine.DeactivateSpeechRecognition();
            }
            else
            {
                SoundIcon = MaterialIconKind.Microphone;
                AI?.SpeechEngine.ActivateSpeechRecognition();
            }
        }

        private void OnLogsClick()
        {
            if (ShowLogs)
            {
                LogsIcon = MaterialIconKind.ClipboardTextOff;
                ShowLogs = false;
            }
            else
            {
                LogsIcon = MaterialIconKind.ClipboardText;
                ShowLogs = true;
            }
        }

        private void OnSettingsClick()
        {
            if (ShowSettings)
            {
                SettingsIcon = MaterialIconKind.CogOff;
                ShowSettings = false;
            }
            else
            {
                SettingsIcon = MaterialIconKind.Cog;
                ShowSettings = true;
            }
        }

        public void SetVisionFrame(byte[] jpeg)
        {
            using MemoryStream ms = new MemoryStream(jpeg);
            VisionFrame = new Avalonia.Media.Imaging.Bitmap(ms);
        }
    }
}
