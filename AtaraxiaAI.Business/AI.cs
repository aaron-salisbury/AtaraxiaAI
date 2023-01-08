using AtaraxiaAI.Business.Base;
using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Business.Services.Base.Models;
using AtaraxiaAI.Data.Domains;
using Desktop.Robot;
using System;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business
{
    public class AI : ObservableObject
    {
        public static InMemoryLogger Log { get; set; }

        internal static InternalStorage InternalStorage { get; set; }
        internal static AppData AppData { get; set; }

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

        public AI()
        {
            _isInitialized = false;

            Log = new InMemoryLogger();

            InternalStorage = Task.Run(async () => await Data.CRUD.ReadInternalStorage(Log.Logger)).Result;
            //TODO: Maybe do a file exists check first instead of letting it fail before creating it for the first time.
            AppData = Task.Run(async () => await Data.CRUD.ReadDataAsync<AppData>(InternalStorage.UserStorageDirectory, Log.Logger)).Result;

            int currentMonth = DateTime.Now.Month;
            if (AppData == null)
            {
                AppData = new AppData { MonthOfLastCloudServicesRoll = currentMonth };
                Task.Run(async () => await Data.CRUD.UpdateDataAsync<AppData>(AppData, InternalStorage.UserStorageDirectory, Log.Logger));
            }
            else if (AppData.MonthOfLastCloudServicesRoll != currentMonth)
            {
                AppData.MonthOfLastCloudServicesRoll = currentMonth;
                AppData.MicrosoftAzureSpeechToTextCharCount = 0;
                AppData.GoogleCloudSpeechToTextByteCount = 0;
                Task.Run(async () => await Data.CRUD.UpdateDataAsync<AppData>(AppData, InternalStorage.UserStorageDirectory, Log.Logger));
            }
        }

        public async Task Initiate(Action<byte[]> updateFrameAction)
        {
            Log.Logger.Information("Initializing ...");

            Log.Logger.Information("... Gathering system data.");
            SystemInfo = new SystemInfo();

            Log.Logger.Information("... Mocking peripherals.");
            Peripherals = new Robot { AutoDelay = 250 };

            Log.Logger.Information("... Acquiring region data.");
            IIPLocationService locationService = new IPAPIIPLocationService();
            Location location = await locationService.GetLocationByIPAsync(SystemInfo.IPAddress);

            Log.Logger.Information("... Verifying ML models.");
            await Data.CRUD.CreateModels(Log.Logger);

            Log.Logger.Information("... Initializing vision engine.");
            VisionEngine = new VisionEngine(updateFrameAction);

            Log.Logger.Information("... Initializing speech engine.");
            SpeechEngine = new SpeechEngine();

            IsInitialized = true;
            Log.Logger.Information("Initialization complete.");
        }

        public string GetUserStorageDirectory()
        {
            return InternalStorage?.UserStorageDirectory;
        }

        public async Task UpdateUserStorageDirectory(string userStorageDirectory)
        {
            await Data.CRUD.UpdateInternalStorage(InternalStorage, Log.Logger, userStorageDirectory);

            // Update AppData in case the user is selecting a network location where they already had it saved.
            AppData = await Data.CRUD.ReadDataAsync<AppData>(InternalStorage.UserStorageDirectory, Log.Logger);
        }

        /// <summary>
        /// Release resources.
        /// </summary>
        public void Shutdown()
        {
            Log.Logger.Information("Shutting down.");

            VisionEngine.Deactivate();
            SpeechEngine.DeactivateSpeechRecognition();
        }
    }
}
