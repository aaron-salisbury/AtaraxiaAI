using AtaraxiaAI.Business.Base;
using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using Desktop.Robot;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business;

public class AI : ObservableObject
{
    private readonly IAudioPlayer _audioPlayer;
    private readonly ILogger _logger;
    private int _shutdownRequested;
    private readonly IIntegrationFactory _integrations;
    private readonly IntegrationDependencies _dependencies;

    private bool _isInitialized;
    public bool IsInitialized
    {
        get => _isInitialized;
        set
        {
            _isInitialized = value;
            RaisePropertyChanged(nameof(IsInitialized));
        }
    }

    public SpeechEngine SpeechEngine { get; set; }
    public VisionEngine VisionEngine { get; set; }

    internal Robot Peripherals { get; set; }

    /// <summary>
    /// Create the AI representation.
    /// </summary>
    /// <param name="logger">The application's logger.</param>
    /// <param name="integrations">Provider selection for external services.</param>
    /// <param name="audioPlayer">Plays synthesized speech.</param>
    /// <param name="dependencies">Dependencies shared by provider adapters.</param>
    public AI(ILogger logger, IIntegrationFactory integrations, IAudioPlayer audioPlayer, IntegrationDependencies dependencies)
    {
        _isInitialized = false;

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _integrations = integrations ?? throw new ArgumentNullException(nameof(integrations));
        _audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
        _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));

    }

    /// <summary>
    /// Initialize the AI componants and other long running tasks.
    /// </summary>
    /// <param name="updateFrameAction">The function that should be called and passed the frame after the vision model processes it.</param>
    public async Task Initiate(Action<byte[]> updateFrameAction)
    {
        _logger.Information("Initializing ...");
        _logger.Information("... Mocking peripherals.");
        Peripherals = new Robot { AutoDelay = 250 };

        _logger.Information("... Using bundled YOLO model.");

        if (Volatile.Read(ref _shutdownRequested) != 0)
        {
            return;
        }

        _logger.Information("... Initializing vision engine.");
        VisionEngine = new VisionEngine(updateFrameAction, _integrations, _logger);

        _logger.Information("... Initializing speech engine.");
        SpeechEngine = new SpeechEngine(_integrations, _audioPlayer, _dependencies);
        await SpeechEngine.SelectSynthesizerAsync(SpeechEngine.DEFAULT_SYNTHESIZER);

        if (Volatile.Read(ref _shutdownRequested) != 0)
        {
            VisionEngine?.Deactivate();
            SpeechEngine?.CancelPlayback();
            SpeechEngine?.DeactivateSpeechRecognition();
            return;
        }

        IsInitialized = true;
        _logger.Information("Initialization complete.");
    }

    /// <summary>
    /// Release resources.
    /// </summary>
    public void Shutdown()
    {
        if (Interlocked.Exchange(ref _shutdownRequested, 1) != 0)
        {
            return;
        }

        _logger.Information("Shutting down.");

        try
        {
            VisionEngine?.Deactivate();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to stop vision capture.");
        }

        try
        {
            SpeechEngine?.CancelPlayback();
            SpeechEngine?.DeactivateSpeechRecognition();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to stop speech recognition.");
        }
    }
}
