using AtaraxiaAI.Business;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

namespace AtaraxiaAI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public RelayCommand OnVisionClickCommand { get; }
        public RelayCommand OnLogsClickCommand { get; }
        public RelayCommand OnSettingsClickCommand { get; }

        public AI AI { get; set; }

        [ObservableProperty]
        private IBitmap? _visionFrame;

        [ObservableProperty]
        private bool _showVisionFeed;

        [ObservableProperty]
        private MaterialIconKind _visionIcon;

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

        public MainWindowViewModel()
        {
            AI = new AI();

            _showVisionFeed = false;
            _visionIcon = MaterialIconKind.EyeOff;
            _showLogs = false;
            _logsIcon = MaterialIconKind.ClipboardTextOff;
            _logsView = App.Current?.Services?.GetService<LogsViewModel>();
            _showSettings = false;
            _settingsIcon = MaterialIconKind.CogOff;
            _settingsView = App.Current?.Services?.GetService<SettingsViewModel>();

            OnVisionClickCommand = new RelayCommand(() => OnVisionClick());
            OnLogsClickCommand = new RelayCommand(() => OnLogsClick());
            OnSettingsClickCommand = new RelayCommand(() => OnSettingsClick());

            Task.Run(() => { AI.Initiate().Wait(); });
        }

        public void Shutdown()
        {
            AI.Shutdown();
        }

        private void OnVisionClick()
        {
            if (AI.IsVisionEngineRunning)
            {
                VisionIcon = MaterialIconKind.EyeOff;
                AI.DeactivateVision();
                ShowVisionFeed = false;
            }
            else
            {
                VisionIcon = MaterialIconKind.Eye;
                AI.ActivateVision(SetVisionFrame);
                ShowVisionFeed = true;
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
