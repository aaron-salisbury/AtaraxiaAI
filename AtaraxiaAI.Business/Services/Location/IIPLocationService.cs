using AtaraxiaAI.Data.Domains;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    internal interface IIPLocationService
    {
        Task<Location> GetLocationByIPAsync(string iPAddress);
    }
}
