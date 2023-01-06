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
    public class AI
    {
        public static InMemoryLogger Log { get; set; }
        public static AppData AppData { get; set; }

        internal SystemInfo SystemInfo { get; set; }
        internal Robot Peripherals { get; set; }
        internal OrchestrationEngine CommandLoop { get; set; }
        internal SpeechEngine SpeechEngine { get; set; }
        internal IVisionEngine VisionEngine { get; set; }

        private Action<byte[]> _updateFrameAction;
        private Action<string> _speechRecognizedAction;

        public bool IsVisionEngineRunning
        {
            get { return _visionTask != null && _visionTask.Status == TaskStatus.Running; }
        }

        private CancellationTokenSource _visionTokenSource;
        private Task _visionTask;

        public AI()
        {
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

            VisionEngine = new PWCYoloVisionEngine();

            SystemInfo = new SystemInfo();
            Log.Logger.Information(SystemInfo.ToString());

            Peripherals = new Robot { AutoDelay = 250 };

            IIPLocationService locationService = new IPAPIIPLocationService();
            Location location = await locationService.GetLocationByIPAsync(SystemInfo.IPAddress);
            Log.Logger.Information(location.ToString());

            SpeechEngine = new SpeechEngine();
            CommandLoop = new OrchestrationEngine(SpeechEngine);
            ActivateSound(CommandLoop.Heard);

            Data.CRUD.CreateModels(Log.Logger);
        }

        public void ActivateVision(Action<byte[]> updateFrameAction)
        {
            _updateFrameAction = updateFrameAction;
            _visionTokenSource = new CancellationTokenSource();
            _visionTask = Task.Run(() => VisionEngine.Initiate(updateFrameAction, _visionTokenSource.Token));
        }

        public void ActivateSound(Action<string> speechRecognizedAction)
        {
            _speechRecognizedAction = speechRecognizedAction;
            SpeechEngine.Recognizer.Listen(_speechRecognizedAction);
        }

        public void DeactivateVision()
        {
            if (IsVisionEngineRunning)
            {
                _visionTokenSource.Cancel();
                _visionTokenSource.Dispose();
                _visionTask.Wait();
                _visionTask.Dispose();
            }
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
            if (SpeechEngine.Recognizer is VoskRecognizer2 voskRecognizer)
            {
                voskRecognizer.CaptureSource = captureSource;
                //TODO: The setter of CaptureSource maybe could do the dispose and reactivate.
                // Same with the vision update, but that one has to care about the token and task.
            }

            SpeechEngine.Recognizer.Dispose();
            ActivateSound(_speechRecognizedAction);
        }

        /// <summary>
        /// Release resources.
        /// </summary>
        public void Shutdown()
        {
            Log.Logger.Information("Shutting down ...");

            DeactivateVision();

            if (SpeechEngine != null)
            {
                SpeechEngine.Recognizer.Dispose();
            }
        }
    }
}
