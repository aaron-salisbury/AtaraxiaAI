using AtaraxiaAI.Business.Base;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Business.Services.VisionEngine;
using AtaraxiaAI.Data.Domains;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business
{
    public class AI
    {
        public static InMemoryLogger Log { get; set; }

        public static AppData AppData { get; set; }

        public IVisionEngine VisionEngine { get; set; }

        public ISpeechEngine SpeechEngine { get; set; }

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
                AppData.GoogleCloudSpeechToTextByteCount = 0;
                Task.Run(async () => await Data.CRUD.UpdateDataAsync<AppData>(AppData, Log.Logger));
            }
        }

        public async Task Initiate(Action<byte[]> updateFrameAction)
        {
            Log.Logger.Information("Initializing ...");

            Log.Logger.Information($"System: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
            //TODO: More system data. Possibly using the following: https://www.nuget.org/packages/System.Management/7.0.0

            IIPAddressService iPService = new IPIFYIPAddressService();
            string iP = await iPService.GetPublicIPAddressAsync();

            IIPLocationService locationService = new IPAPIIPLocationService();
            Data.Domains.Location location = await locationService.GetLocationByIPAsync(iP);

            SpeechEngine = new DotNetSpeechEngine();
            CommandLoop = new OrchestrationEngine(SpeechEngine);
            SpeechEngine.Listen(CommandLoop.Heard);

            VisionEngine = new PWCYoloVisionEngine();
            VisionEngine.Initiate(updateFrameAction);
        }
    }
}
