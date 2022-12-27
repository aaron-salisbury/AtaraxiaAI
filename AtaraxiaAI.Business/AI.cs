using AtaraxiaAI.Business.Base;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Business.Services.VisionEngine;
using AtaraxiaAI.Business.Skills;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business
{
    public class AI
    {
        public static InMemoryLogger Log { get; set; }

        public byte[] CurrentVisionFrameJpeg { get; set; }

        public IVisionEngine VisionEngine { get; set; }

        public ISpeechEngine SpeechEngine { get; set; }

        public AI()
        {
            Log = new InMemoryLogger();
        }

        public async Task Initiate(Action<byte[]> updateFrameAction)
        {
            Log.Logger.Information("Initializing ...");
            Log.Logger.Information($"System: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");

            VisionEngine = new PWCYoloVisionEngine();
            Task.Run(() => { VisionEngine.Initiate(updateFrameAction); });

            SpeechEngine = new DotNetSpeechEngine();
            SpeechEngine.Listen(this.Heard);

            IIPAddressService ipService = new IPIFYIPAddressService();
            string iP = await ipService.GetPublicIPAddressAsync();

            IIPLocationService locationService = new IPAPIIPLocationService();
            AtaraxiaAI.Data.Domains.Location location = await locationService.GetLocationByIPAsync(iP);
            if (location != null)
            {
                Log.Logger.Information($"Approximate Location: {location.City}, {location.Region} {location.Zip}");
            }
        }

        private void Heard(string message)
        {
            Log.Logger.Information($"Heard \"{message}\".");

            switch (message.ToLower())
            {
                case "tell me a joke":
                    Sv443Joke joke = Sv443Jokes.GetJokeAsync().Result;
                    if (joke != null && !string.IsNullOrEmpty(joke.Joke))
                    {
                        SpeechEngine.Speek(joke.Joke);
                    }
                    break;
                case "tell me a dark joke":
                    Sv443Joke darkJoke = Sv443Jokes.GetDarkJokeAsync().Result;
                    if (darkJoke != null && !string.IsNullOrEmpty(darkJoke.Joke))
                    {
                        SpeechEngine.Speek(darkJoke.Joke);
                    }
                    break;
            }
        }
    }
}
