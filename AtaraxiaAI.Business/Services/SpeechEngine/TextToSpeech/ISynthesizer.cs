using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    public interface ISynthesizer
    {
        bool IsAvailable();

        Task SpeakAsync(string message);
    }
}
