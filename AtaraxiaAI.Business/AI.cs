using AtaraxiaAI.Business.Base;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Data.Domains;
using System;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business
{
    public class AI
    {
        public static InMemoryLogger Log { get; set; }
        public static AppData AppData { get; set; }

        public SystemInfo SystemInfo { get; set; }
        public IVisionEngine VisionEngine { get; set; }
        public SpeechEngine SpeechEngine { get; set; }
        public OrchestrationEngine CommandLoop { get; set; }

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

        public async Task Initiate(Action<byte[]> updateFrameAction)
        {
            Log.Logger.Information("Initializing ...");

            SystemInfo = new SystemInfo();
            Log.Logger.Information(SystemInfo.ToString());

            IIPLocationService locationService = new IPAPIIPLocationService();
            Data.Domains.Location location = await locationService.GetLocationByIPAsync(SystemInfo.IPAddress);
            Log.Logger.Information($"Location: {location.City}, {location.Region} {location.Zip}");

            SpeechEngine = new SpeechEngine();
            CommandLoop = new OrchestrationEngine(SpeechEngine);
            SpeechEngine.Recognizer.Listen(CommandLoop.Heard);

            VisionEngine = new PWCYoloVisionEngine();
            VisionEngine.Initiate(updateFrameAction);
        }
    }
}
