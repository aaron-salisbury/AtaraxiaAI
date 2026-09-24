using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    public interface IAnswerProvider
    {
        bool IsAvailable();

        Task<string> AnswerAsync(string message);
    }
}
