using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Integrations.Services;
using System.Globalization;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Integrations
{
    public sealed class IntegrationFactory : IIntegrationFactory
    {
        private readonly IntegrationDependencies _dependencies;

        public IntegrationFactory(IntegrationDependencies dependencies) => _dependencies = dependencies;
        public Task CreateModelsAsync() => ModelDownloader.CreateModels(_dependencies.HttpRequester, _dependencies.Logger);
        public IGeneralIntelligence CreateGeneralIntelligence() => new GPT3GeneralIntelligence(_dependencies);
        public IIPAddressService CreateIPAddressService() => new IPIFYIPAddressService(_dependencies);
        public IIPLocationService CreateLocationService() => new IPAPIIPLocationService(_dependencies);
        public IInsultService CreateInsultService() => new EvilInsultService(_dependencies);
        public IJokeService CreateJokeService(bool dadJoke = false) => dadJoke ? new CanHazDadJokeService(_dependencies) : new Sv443JokeService(_dependencies);
        public IObjectDetector CreateObjectDetector() => new YoloObjectDetector(_dependencies);
        public IOpticalCharacterRecognizer CreateOpticalCharacterRecognizer() => new TesseractOCR(_dependencies);
        public IRecognizer CreateRecognizer(CultureInfo culture) => new SystemDotSpeechRecognizer(culture);
        public IStreamingAvailabilityService CreateStreamingAvailabilityService() => new WatchModeStreamingAvailabilityService(_dependencies);
        public ISynthesizer CreateSynthesizer(SpeechSynthesizers synthesizer, CultureInfo culture) => synthesizer switch
        {
            SpeechSynthesizers.Kokoro => new KokoroSynthesizer(culture, _dependencies),
            SpeechSynthesizers.GoogleCloud => new GoogleCloudSynthesizer(culture, _dependencies),
            SpeechSynthesizers.MicrosoftAzure => new MicrosoftAzureSynthesizer(culture, _dependencies),
            SpeechSynthesizers.MicrosoftBing => new MicrosoftBingSynthesizer(culture, _dependencies),
            SpeechSynthesizers.SystemDotSpeech => new SystemDotSpeechSynthesizer(culture, _dependencies),
            _ => throw new System.ArgumentOutOfRangeException(nameof(synthesizer))
        };
    }
}
