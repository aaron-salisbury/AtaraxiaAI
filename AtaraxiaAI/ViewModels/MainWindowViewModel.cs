using AtaraxiaAI.Business;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;

namespace AtaraxiaAI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private const string BKGRND_GIF_PATH = "avares://AtaraxiaAI/Base/Assets/278284.gif";

        public RelayCommand OnVisionClickCommand { get; }

        [ObservableProperty]
        private object _greeting;

        [ObservableProperty]
        private AI _aI;

        [ObservableProperty]
        private string _logMessages;

        [ObservableProperty]
        private string? _backgroundGifPath;

        [ObservableProperty]
        private bool _isBackgroundGifVisible;

        [ObservableProperty]
        private IBitmap? _visionFrame;

        [ObservableProperty]
        private bool _isVisionFrameVisible;

        public MainWindowViewModel()
        {
            _isBackgroundGifVisible = true;
            _isVisionFrameVisible = false;
            _greeting = "Welcome to Avalonia!";
            _backgroundGifPath = BKGRND_GIF_PATH;
            _logMessages = string.Empty;
            _aI = new AI();

            AI.Log.InMemorySink.Messages.CollectionChanged += new NotifyCollectionChangedEventHandler(OnLogsPropertyChanged);

            Task.Run(() => { AI.Initiate().Wait(); });

            OnVisionClickCommand = new RelayCommand(() => OnVisionClick());
        }

        private void OnLogsPropertyChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            LogMessages = string.Concat(AI.Log.InMemorySink.Messages);
        }

        private void OnVisionClick()
        {
            if (AI.VisionEngine.IsActive())
            {
                AI.DeactivateVision();
                BackgroundGifPath = BKGRND_GIF_PATH;
                _isBackgroundGifVisible = true;
                _isVisionFrameVisible = false;
            }
            else
            {
                AI.ActivateVision(SetVisionFrame);
                _isBackgroundGifVisible = false;
                _isVisionFrameVisible = true; //TODO: This isnt working for some reason.
            }
        }

        public void SetVisionFrame(byte[] jpeg)
        {
            using MemoryStream ms = new MemoryStream(jpeg);
            VisionFrame = new Avalonia.Media.Imaging.Bitmap(ms);
        }
    }
}
