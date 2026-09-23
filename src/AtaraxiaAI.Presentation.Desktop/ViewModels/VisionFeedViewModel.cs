using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using System.Threading;

namespace AtaraxiaAI.Presentation.Desktop.ViewModels;

public partial class VisionFeedViewModel : BaseViewModel
{
    private byte[]? _pendingFrame;
    private int _frameUpdateScheduled;
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
        Interlocked.Exchange(ref _pendingFrame, jpeg);
        if (Interlocked.Exchange(ref _frameUpdateScheduled, 1) != 0) return;

        Dispatcher.UIThread.Post(() =>
        {
            byte[]? latest = Interlocked.Exchange(ref _pendingFrame, null);
            Interlocked.Exchange(ref _frameUpdateScheduled, 0);
            if (ShowCameraFeed) FrameBuffer = latest;
        });
    }
}
