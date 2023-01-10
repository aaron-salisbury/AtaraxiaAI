using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using System.IO;

namespace AtaraxiaAI.ViewModels
{
    public partial class VisionFeedViewModel : ObservableObject
    {
        public RelayCommand OnCameraClickCommand { get; }

        [ObservableProperty]
        private IBitmap? _visionFrame;

        [ObservableProperty]
        private bool _showCameraFeed;

        [ObservableProperty]
        private MaterialIconKind _cameraIcon;

        public VisionFeedViewModel()
        {
            _showCameraFeed = true;
            _cameraIcon = MaterialIconKind.Video;

            OnCameraClickCommand = new RelayCommand(() => OnCameraClick());
        }

        private void OnCameraClick()
        {
            if (ShowCameraFeed)
            {
                CameraIcon = MaterialIconKind.VideoOff;
                ShowCameraFeed = false;
                VisionFrame = null;
            }
            else
            {
                CameraIcon = MaterialIconKind.Video;
                ShowCameraFeed = true;
            }
        }

        public void SetVisionFrame(byte[] jpeg)
        {
            if (ShowCameraFeed)
            {
                using MemoryStream ms = new MemoryStream(jpeg);
                VisionFrame = new Avalonia.Media.Imaging.Bitmap(ms);
            }
        }
    }
}
