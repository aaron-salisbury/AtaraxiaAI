using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    internal interface IIPAddressService
    {
        Task<string> GetPublicIPAddressAsync();
    }
}
