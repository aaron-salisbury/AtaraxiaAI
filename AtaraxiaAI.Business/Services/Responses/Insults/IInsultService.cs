using AtaraxiaAI.Business.Services.Base.Domains;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    internal interface IInsultService
    {
        Task<string> GetInsultAsync();
    }
}
