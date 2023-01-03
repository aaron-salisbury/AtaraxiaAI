using AtaraxiaAI.Business.Services.Base.Models;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    internal interface IIPLocationService
    {
        Task<Location> GetLocationByIPAsync(string iPAddress);
    }
}
