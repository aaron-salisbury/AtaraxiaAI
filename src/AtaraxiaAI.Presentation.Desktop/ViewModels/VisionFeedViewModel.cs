using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;

namespace AtaraxiaAI.Presentation.Desktop.ViewModels;

public partial class VisionFeedViewModel : BaseViewModel
{
    public RelayCommand OnCameraClickCommand { get; }

    [ObservableProperty]
    private byte[]? _frameBuffer;

    [ObservableProperty]
    private bool _showCameraFeed;

    [ObservableProperty]
    private string _cameraIcon;

    public VisionFeedViewModel()
    {
        _showCameraFeed = true;
        _cameraIcon = "Video";

        OnCameraClickCommand = new RelayCommand(() => OnCameraClick());
    }

    private void OnCameraClick()
    {
        if (ShowCameraFeed)
        {
            CameraIcon = "VideoOff";
            ShowCameraFeed = false;
            FrameBuffer = null;
        }
        else
        {
            CameraIcon = "Video";
            ShowCameraFeed = true;
        }
    }

    public void SetVisionFrame(byte[] jpeg)
    {
        if (ShowCameraFeed)
        {
            FrameBuffer = jpeg;
        }
    }
}
