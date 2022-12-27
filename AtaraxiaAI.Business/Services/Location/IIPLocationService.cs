using AtaraxiaAI.Data.Domains;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    public interface IIPLocationService
    {
        Task<Location> GetLocationByIPAsync(string iPAddress);
    }
}
