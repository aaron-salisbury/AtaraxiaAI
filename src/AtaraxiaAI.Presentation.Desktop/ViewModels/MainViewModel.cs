using AtaraxiaAI.Business;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using Serilog;
using System;
using System.Threading.Tasks;

namespace AtaraxiaAI.Presentation.Desktop.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    public RelayCommand OnVisionClickCommand { get; }
    public RelayCommand OnSoundClickCommand { get; }
    public RelayCommand OnLogsClickCommand { get; }
    public RelayCommand OnSettingsClickCommand { get; }

    public AI AI { get; }
    private readonly SettingsViewModel _settings;

    [ObservableProperty]
    private string? _initializationError;

    [ObservableProperty]
    private bool _isInitialized;

    [ObservableProperty]
    private bool _isInitializing = true;

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

    public MainViewModel(AI ai, LogsViewModel logs, SettingsViewModel settings, VisionFeedViewModel vision)
    {
        AI = ai;
        _settings = settings;

        _activateVision = false;
        _visionIcon = "EyeOff";
        _soundIcon = "MicOff";
        _logsView = logs;
        _showLogs = true;
        _logsIcon = "ClipboardText";
        _settingsView = settings;
        _showSettings = false;
        _settingsIcon = "CogOff";

        _visionFeedView = vision;

        OnVisionClickCommand = new RelayCommand(() => OnVisionClick());
        OnSoundClickCommand = new RelayCommand(() => OnSoundClick());
        OnLogsClickCommand = new RelayCommand(() => OnLogsClick());
        OnSettingsClickCommand = new RelayCommand(() => OnSettingsClick());

        _ = InitializeAsync(vision);
    }

    private async Task InitializeAsync(VisionFeedViewModel vision)
    {
        try
        {
            await Task.Run(() => AI.Initiate(updateFrameAction: vision.SetVisionFrame));
            IsInitialized = AI.IsInitialized;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize AtaraxiaAI.");
            InitializationError = ex.Message;
        }
        finally
        {
            IsInitializing = false;
        }
    }

    public void Shutdown() => AI.Shutdown();

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
