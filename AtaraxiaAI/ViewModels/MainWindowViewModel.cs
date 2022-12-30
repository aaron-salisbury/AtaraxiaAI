using AtaraxiaAI.Business;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;

namespace AtaraxiaAI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _greeting;

        [ObservableProperty]
        private AI _aI;

        [ObservableProperty]
        private string _logMessages;

        [ObservableProperty]
        private string? _backgroundGifPath;

        [ObservableProperty]
        private IBitmap? _visionFrame;

        public MainWindowViewModel()
        {
            _greeting = "Welcome to Avalonia!";
            _backgroundGifPath = "avares://AtaraxiaAI/Base/Assets/278284.gif";
            _logMessages = string.Empty;
            _aI = new AI();

            AI.Log.InMemorySink.Messages.CollectionChanged += new NotifyCollectionChangedEventHandler(OnLogsPropertyChanged);

            Task.Run(() => { AI.Initiate(SetVisionFrame).Wait(); });
        }

        private void OnLogsPropertyChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            LogMessages = string.Concat(AI.Log.InMemorySink.Messages);
        }

        public void SetVisionFrame(byte[] jpeg)
        {
            using MemoryStream ms = new MemoryStream(jpeg);
            VisionFrame = new Avalonia.Media.Imaging.Bitmap(ms);

            BackgroundGifPath = null;
        }
    }
}
