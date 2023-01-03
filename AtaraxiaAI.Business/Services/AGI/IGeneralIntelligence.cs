using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    internal interface IGeneralIntelligence
    {
        bool IsAvailable();

        Task<string> AnswerMe(string message);
    }
}
