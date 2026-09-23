using System.Globalization;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Business.Services
{
    public interface IIntegrationFactory
    {
        Task CreateModelsAsync();
        IGeneralIntelligence CreateGeneralIntelligence();
        IIPAddressService CreateIPAddressService();
        IIPLocationService CreateLocationService();
        IInsultService CreateInsultService();
        IJokeService CreateJokeService(bool dadJoke = false);
        IObjectDetector CreateObjectDetector();
        IOpticalCharacterRecognizer CreateOpticalCharacterRecognizer();
        IRecognizer CreateRecognizer(CultureInfo culture);
        IStreamingAvailabilityService CreateStreamingAvailabilityService();
        ISynthesizer CreateSynthesizer(SpeechSynthesizers synthesizer, CultureInfo culture);
    }
}
