using AtaraxiaAI.Business.Base;
using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Persistence;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Business.Services.Base.Models;
using Desktop.Robot;
using RunnethOverStudio.AppToolkit.Modules.Access;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business
{
    public class AI : ObservableObject
    {
        private readonly IAppDataStore _store;
        private readonly IAudioPlayer _audioPlayer;
        private readonly ILogger _logger;
        private int _shutdownRequested;
        private readonly IIntegrationFactory _integrations;
        private readonly IntegrationDependencies _dependencies;
        private InternalStorage InternalStorage { get; set; }
        private AppData AppData { get; set; }

        private bool _isInitialized;
        public bool IsInitialized
        {
            get { return _isInitialized; }
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
        /// <param name="store">Local application data store.</param>
        public AI(ILogger logger, IIntegrationFactory integrations, IAppDataStore store, IAudioPlayer audioPlayer, IntegrationDependencies dependencies)
        {
            _isInitialized = false;

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _integrations = integrations ?? throw new ArgumentNullException(nameof(integrations));
            _store = store ?? throw new ArgumentNullException(nameof(store));
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
            await InitializeStorageAsync();
            if (Volatile.Read(ref _shutdownRequested) != 0) return;
            _logger.Information("... Mocking peripherals.");
            Peripherals = new Robot { AutoDelay = 250 };

            _logger.Information("... Using bundled YOLO model; preparing Kokoro in the background.");
            _ = _integrations.PrepareSpeechAsync();

            if (Volatile.Read(ref _shutdownRequested) != 0) return;

            _logger.Information("... Initializing vision engine.");
            VisionEngine = new VisionEngine(updateFrameAction, _integrations, _logger);

            _logger.Information("... Initializing speech engine.");
            SpeechEngine = new SpeechEngine(_integrations, _audioPlayer, _dependencies);

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

        internal async Task InitializeStorageAsync()
        {
            _logger.Information("... Loading application data.");
            InternalStorage = await _store.ReadInternalStorageAsync();
            AppData = await _store.ReadAppDataAsync(InternalStorage.UserStorageDirectory) ?? new AppData();
            _dependencies.AppData = AppData;
            await RefreshQuotasAsync();
            _logger.Information("... Application data loaded.");
        }

        /// <summary>
        /// Get the directory path where the application data gets saved.
        /// </summary>
        public string GetUserStorageDirectory()
        {
            return InternalStorage?.UserStorageDirectory;
        }

        /// <summary>
        /// Change the directory path where the application data gets saved.
        /// Moves existing application data when the destination is empty.
        /// If the destination already has data, both files are preserved and the destination becomes active.
        /// </summary>
        public async Task UpdateUserStorageDirectory(string newUserStorageDirectory)
        {
            InternalStorage = await _store.UpdateInternalStorageAsync(InternalStorage, newUserStorageDirectory);

            // Update AppData in case the user is selecting a network location where they already had it saved.
            AppData preExistingAppData = await _store.ReadAppDataAsync(newUserStorageDirectory);
            if (preExistingAppData != null)
            {
                AppData = preExistingAppData;
                _dependencies.AppData = AppData;
            }
        }

        /// <summary>
        /// Release resources and update stored application data.
        /// </summary>
        public void Shutdown()
        {
            if (Interlocked.Exchange(ref _shutdownRequested, 1) != 0) return;
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

            if (AppData != null && InternalStorage != null)
                _store.SaveAppDataAsync(AppData, InternalStorage.UserStorageDirectory).GetAwaiter().GetResult();
        }
        private async Task RefreshQuotasAsync()
        {
            DateTime today = DateTime.Today;
            bool changed = false;
            if (AppData.MonthOfLastCloudServicesRoll != today.Month)
            {
                AppData.MonthOfLastCloudServicesRoll = today.Month;
                AppData.MicrosoftAzureSpeechToTextCharCount = 0;
                AppData.GoogleCloudSpeechToTextByteCount = 0;
                changed = true;
            }
            if (AppData.WatchmodeQuotaResetsOn is DateTime reset && reset.Date < today)
            {
                while (reset.Date < today) reset = reset.AddMonths(1);
                AppData.WatchmodeQuotaResetsOn = reset;
                AppData.WatchmodeCurrentAPIUsage = 0;
                changed = true;
            }
            if (changed) await _store.SaveAppDataAsync(AppData, InternalStorage.UserStorageDirectory);
        }
    }
}
