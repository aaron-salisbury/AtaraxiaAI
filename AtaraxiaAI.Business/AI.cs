using AtaraxiaAI.Business.Base;
using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Data.Domains;
using Desktop.Robot;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business
{
    public class AI
    {
        public static InMemoryLogger Log { get; set; }
        public static AppData AppData { get; set; }

        public SystemInfo SystemInfo { get; set; }
        public Robot Peripherals { get; set; }
        public OrchestrationEngine CommandLoop { get; set; }
        public SpeechEngine SpeechEngine { get; set; }
        public IVisionEngine VisionEngine { get; set; }

        public bool IsVisionEngineRunning
        {
            get { return _visionTask != null && _visionTask.Status == TaskStatus.Running; }
        }

        private CancellationTokenSource _visionTokenSource;
        private Task _visionTask;

        public AI()
        {
            Log = new InMemoryLogger();
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
            Data.Domains.Location location = await locationService.GetLocationByIPAsync(SystemInfo.IPAddress);
            Log.Logger.Information($"Location: {location.City}, {location.Region} {location.Zip}");

            SpeechEngine = new SpeechEngine();
            CommandLoop = new OrchestrationEngine(SpeechEngine);
            SpeechEngine.Recognizer.Listen(CommandLoop.Heard);
        }

        public void ActivateVision(Action<byte[]> updateFrameAction)
        {
            _visionTokenSource = new CancellationTokenSource();
            _visionTask = Task.Run(() => VisionEngine.Initiate(updateFrameAction, _visionTokenSource.Token));
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

        /// <summary>
        /// Release resources.
        /// </summary>
        public void Shutdown()
        {
            DeactivateVision();
        }
    }
}
