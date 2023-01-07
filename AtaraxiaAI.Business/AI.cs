using AtaraxiaAI.Business.Base;
using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Business.Services.Base.Models;
using AtaraxiaAI.Data.Domains;
using Desktop.Robot;
using System;
using System.Threading;
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

        public bool IsSpeechRecognitionRunning { get; set; }
        public bool IsVisionEngineRunning
        {
            get { return _visionTask != null && _visionTask.Status == TaskStatus.Running; }
        }

        internal SystemInfo SystemInfo { get; set; }
        internal Robot Peripherals { get; set; }
        internal OrchestrationEngine CommandLoop { get; set; }
        internal SpeechEngine SpeechEngine { get; set; }
        internal IVisionEngine VisionEngine { get; set; }

        private Action<byte[]> _updateFrameAction;
        private Action<string> _speechRecognizedAction;
        private CancellationTokenSource _visionTokenSource;
        private Task _visionTask;

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

        public async Task Initiate()
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
            VisionEngine = new PWCYoloVisionEngine();

            Log.Logger.Information("... Initializing speech engine.");
            SpeechEngine = new SpeechEngine();
            CommandLoop = new OrchestrationEngine(SpeechEngine);

            IsInitialized = true;
            Log.Logger.Information("Initialization complete.");
        }

        public void ActivateVision(Action<byte[]> updateFrameAction)
        {
            Log.Logger.Information("Beginning object detection.");
            _updateFrameAction = updateFrameAction;
            _visionTokenSource = new CancellationTokenSource();
            _visionTask = Task.Run(() => VisionEngine.Initiate(updateFrameAction, _visionTokenSource.Token));
        }

        public void ActivateSpeechRecognition()
        {
            Log.Logger.Information("Beginning speech recognition.");
            _speechRecognizedAction = CommandLoop.Heard;
            SpeechEngine.Recognizer.Listen(_speechRecognizedAction);
            IsSpeechRecognitionRunning = true;
        }

        public void DeactivateVision()
        {
            if (IsVisionEngineRunning)
            {
                _visionTokenSource.Cancel();
                _visionTokenSource.Dispose();
                _visionTask.Wait();
                _visionTask.Dispose();
                Log.Logger.Information("Ended object detection.");
            }
        }

        public void DeactivateSpeechRecognition()
        {
            SpeechEngine?.Recognizer.Dispose();
            Log.Logger.Information("Ended speech recognition.");
            IsSpeechRecognitionRunning = false;
        }

        public void UpdateVisionCaptureSource(VisionCaptureSources captureSource)
        {
            if (VisionEngine is PWCYoloVisionEngine yoloEngine)
            {
                yoloEngine.CaptureSource = captureSource;
            }

            if (IsVisionEngineRunning)
            {
                DeactivateVision();
                ActivateVision(_updateFrameAction);
            }
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

            DeactivateVision();
            DeactivateSpeechRecognition();
        }
    }
}
