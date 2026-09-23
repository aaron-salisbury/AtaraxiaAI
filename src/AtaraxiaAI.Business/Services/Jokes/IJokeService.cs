using AtaraxiaAI.Business.Services.Base.Models;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    public interface IJokeService
    {
        Task<Joke> GetJokeAsync();
    }
}
