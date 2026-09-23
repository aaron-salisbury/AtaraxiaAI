using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    public interface IGeneralIntelligence
    {
        bool IsAvailable();

        Task<string> AnswerMe(string message);
    }
}
