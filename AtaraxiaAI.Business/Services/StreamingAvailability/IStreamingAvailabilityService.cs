using System.Collections.Generic;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    internal interface IStreamingAvailabilityService
    {
        Task<IEnumerable<string>> GetTVShowStreamOfferingsAsync(string title);

        Task<IEnumerable<string>> GetMovieStreamOfferingsAsync(string title);
    }
}
