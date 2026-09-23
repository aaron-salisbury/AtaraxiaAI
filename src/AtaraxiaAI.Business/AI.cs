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
        internal static ILogger Logger { get; private set; }
        private readonly IAppDataStore _store;
        private readonly IAudioPlayer _audioPlayer;
        private int _shutdownRequested;
        internal static IIntegrationFactory Integrations { get; private set; }
        internal static IHttpRequester HttpRequester { get; private set; }
        internal static InternalStorage InternalStorage { get; private set; }
        internal static AppData AppData { get; private set; }

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

        internal SystemInfo SystemInfo { get; set; }
        internal Robot Peripherals { get; set; }

        /// <summary>
        /// Create the AI representation.
        /// </summary>
        /// <param name="logger">The application's logger.</param>
        /// <param name="integrations">Provider selection for external services.</param>
        /// <param name="httpRequester">HTTP requests used by integration adapters.</param>
        /// <param name="store">Local application data store.</param>
        public AI(ILogger logger, IIntegrationFactory integrations, IHttpRequester httpRequester, IAppDataStore store, IAudioPlayer audioPlayer)
        {
            _isInitialized = false;

            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Integrations = integrations ?? throw new ArgumentNullException(nameof(integrations));
            HttpRequester = httpRequester ?? throw new ArgumentNullException(nameof(httpRequester));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
            // Settings reads the storage directory during window construction.
            Task.Run(async () =>
            {
                InternalStorage = await _store.ReadInternalStorageAsync();
                AppData = await _store.ReadAppDataAsync(InternalStorage.UserStorageDirectory) ?? new AppData();
                await RefreshQuotasAsync();
            }).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initialize the AI componants and other long running tasks.
        /// </summary>
        /// <param name="updateFrameAction">The function that should be called and passed the frame after the vision model processes it.</param>
        public async Task Initiate(Action<byte[]> updateFrameAction)
        {
            Logger.Information("Initializing ...");
            Logger.Information("... Gathering system data.");
            SystemInfo = new SystemInfo();

            Logger.Information("... Mocking peripherals.");
            Peripherals = new Robot { AutoDelay = 250 };

            Logger.Information("... Acquiring region data.");
            IIPLocationService locationService = AI.Integrations.CreateLocationService();
            Location location = await locationService.GetLocationByIPAsync(SystemInfo.IPAddress);

            Logger.Information("... Verifying ML models.");
            await Integrations.CreateModelsAsync();

            if (Volatile.Read(ref _shutdownRequested) != 0) return;

            Logger.Information("... Initializing vision engine.");
            VisionEngine = new VisionEngine(updateFrameAction);

            Logger.Information("... Initializing speech engine.");
            SpeechEngine = new SpeechEngine(Integrations, _audioPlayer);

            if (Volatile.Read(ref _shutdownRequested) != 0)
            {
                VisionEngine?.Deactivate();
                SpeechEngine?.CancelPlayback();
                SpeechEngine?.DeactivateSpeechRecognition();
                return;
            }

            IsInitialized = true;
            Logger.Information("Initialization complete.");
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
            }
        }

        /// <summary>
        /// Release resources and update stored application data.
        /// </summary>
        public void Shutdown()
        {
            if (Interlocked.Exchange(ref _shutdownRequested, 1) != 0) return;
            Logger.Information("Shutting down.");

            try
            {
                VisionEngine?.Deactivate();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to stop vision capture.");
            }

            try
            {
                SpeechEngine?.CancelPlayback();
                SpeechEngine?.DeactivateSpeechRecognition();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to stop speech recognition.");
            }

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
