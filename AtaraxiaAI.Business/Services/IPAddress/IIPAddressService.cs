using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    public interface IIPAddressService
    {
        Task<string> GetPublicIPAddressAsync();
    }
}
