using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Integrations.Services;
using System.Globalization;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Integrations
{
    public sealed class IntegrationFactory : IIntegrationFactory
    {
        public Task CreateModelsAsync() => ModelDownloader.CreateModels(AI.HttpRequester, AI.Logger);
        public IGeneralIntelligence CreateGeneralIntelligence() => new GPT3GeneralIntelligence();
        public IIPAddressService CreateIPAddressService() => new IPIFYIPAddressService();
        public IIPLocationService CreateLocationService() => new IPAPIIPLocationService();
        public IInsultService CreateInsultService() => new EvilInsultService();
        public IJokeService CreateJokeService(bool dadJoke = false) => dadJoke ? new CanHazDadJokeService() : new Sv443JokeService();
        public IObjectDetector CreateObjectDetector() => new YoloObjectDetector();
        public IOpticalCharacterRecognizer CreateOpticalCharacterRecognizer() => new TesseractOCR();
        public IRecognizer CreateRecognizer(CultureInfo culture) => new SystemDotSpeechRecognizer(culture);
        public IStreamingAvailabilityService CreateStreamingAvailabilityService() => new WatchModeStreamingAvailabilityService();
        public ISynthesizer CreateSynthesizer(SpeechSynthesizers synthesizer, CultureInfo culture) => synthesizer switch
        {
            SpeechSynthesizers.GoogleCloud => new GoogleCloudSynthesizer(culture),
            SpeechSynthesizers.MicrosoftAzure => new MicrosoftAzureSynthesizer(culture),
            SpeechSynthesizers.MicrosoftBing => new MicrosoftBingSynthesizer(culture),
            SpeechSynthesizers.SystemDotSpeech => new SystemDotSpeechSynthesizer(culture),
            _ => throw new System.ArgumentOutOfRangeException(nameof(synthesizer))
        };
    }
}
