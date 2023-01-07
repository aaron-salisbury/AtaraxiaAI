using AtaraxiaAI.Business.Base;
using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Business.Services.Base.Models;
using AtaraxiaAI.Data.Domains;
using Desktop.Robot;
using System;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Business
{
    public class AI : ObservableObject
    {
        public static InMemoryLogger Log { get; set; }
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

        public VisionEngine VisionEngine { get; set; }
        public bool IsSpeechRecognitionRunning { get; set; }

        internal SystemInfo SystemInfo { get; set; }
        internal Robot Peripherals { get; set; }
        internal OrchestrationEngine CommandLoop { get; set; }
        internal SpeechEngine SpeechEngine { get; set; }

        private Action<string> _speechRecognizedAction;

        public AI()
        {
            _isInitialized = false;

            Log = new InMemoryLogger();
            //TODO: Maybe do a file exists check first instead of letting it fail before creating it for the first time.
            AppData = Task.Run(async () => await Data.CRUD.ReadDataAsync<AppData>(Log.Logger)).Result;

            int currentMonth = DateTime.Now.Month;
            if (AppData == null)
            {
                AppData = new AppData { MonthOfLastCloudServicesRoll = currentMonth };
                Task.Run(async () => await Data.CRUD.UpdateDataAsync<AppData>(AppData, Log.Logger));
            }
            else if (AppData.MonthOfLastCloudServicesRoll != currentMonth)
            {
                AppData.MonthOfLastCloudServicesRoll = currentMonth;
                AppData.MicrosoftAzureSpeechToTextCharCount = 0;
                AppData.GoogleCloudSpeechToTextByteCount = 0;
                Task.Run(async () => await Data.CRUD.UpdateDataAsync<AppData>(AppData, Log.Logger));
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
            Data.CRUD.CreateModels(Log.Logger);

            Log.Logger.Information("... Initializing vision engine.");
            VisionEngine = new VisionEngine(updateFrameAction);

            Log.Logger.Information("... Initializing speech engine.");
            SpeechEngine = new SpeechEngine();
            CommandLoop = new OrchestrationEngine(SpeechEngine);

            IsInitialized = true;
            Log.Logger.Information("Initialization complete.");
        }

        public void ActivateSpeechRecognition()
        {
            Log.Logger.Information("Beginning speech recognition.");
            _speechRecognizedAction = CommandLoop.Heard;
            SpeechEngine.Recognizer.Listen(_speechRecognizedAction);
            IsSpeechRecognitionRunning = true;
        }

        public void DeactivateSpeechRecognition()
        {
            SpeechEngine?.Recognizer.Dispose();
            Log.Logger.Information("Ended speech recognition.");
            IsSpeechRecognitionRunning = false;
        }

        public void UpdateSoundCaptureSource(SoundCaptureSources captureSource)
        {
            if (SpeechEngine.Recognizer is VoskRecognizer voskRecognizer)
            {
                voskRecognizer.CaptureSource = captureSource;
            }

            if (IsSpeechRecognitionRunning)
            {
                DeactivateSpeechRecognition();
                ActivateSpeechRecognition();
            }
        }

        /// <summary>
        /// Release resources.
        /// </summary>
        public void Shutdown()
        {
            Log.Logger.Information("Shutting down.");

            VisionEngine.Deactivate();
            DeactivateSpeechRecognition();
        }
    }
}
