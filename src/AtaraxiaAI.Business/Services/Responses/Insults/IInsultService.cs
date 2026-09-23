using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    public interface IInsultService
    {
        Task<string> GetInsultAsync();
    }
}
