using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    public interface ISynthesizer
    {
        bool IsAvailable();

        Task<bool> SpeakAsync(string message);

        // https://github.com/voxell-tech/UnityTTS
    }
}
