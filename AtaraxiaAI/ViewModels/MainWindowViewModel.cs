using AtaraxiaAI.Business;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;

namespace AtaraxiaAI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private const string BKGRND_GIF_PATH = "avares://AtaraxiaAI/Base/Assets/278284.gif";

        public RelayCommand OnVisionClickCommand { get; }
        public RelayCommand OnLogsClickCommand { get; }
        public RelayCommand OnSettingsClickCommand { get; }

        public AI AI { get; set; }

        [ObservableProperty]
        private string _logMessages;

        [ObservableProperty]
        private string? _backgroundGifPath;

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
        private bool _showSettings;

        [ObservableProperty]
        private MaterialIconKind _settingsIcon;

        public MainWindowViewModel()
        {
            _showVisionFeed = false;
            _visionIcon = MaterialIconKind.EyeOff;
            _showLogs = false;
            _logsIcon = MaterialIconKind.ClipboardTextOff;
            _showSettings = false;
            _settingsIcon = MaterialIconKind.CogOff;
            _backgroundGifPath = BKGRND_GIF_PATH;
            _logMessages = string.Empty;

            OnVisionClickCommand = new RelayCommand(() => OnVisionClick());
            OnLogsClickCommand = new RelayCommand(() => OnLogsClick());
            OnSettingsClickCommand = new RelayCommand(() => OnSettingsClick());

            AI = new AI();
            AI.Log.InMemorySink.Messages.CollectionChanged += new NotifyCollectionChangedEventHandler(OnLogsPropertyChanged);
            Task.Run(() => { AI.Initiate().Wait(); });
        }

        private void OnLogsPropertyChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            LogMessages = string.Concat(AI.Log.InMemorySink.Messages);
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
