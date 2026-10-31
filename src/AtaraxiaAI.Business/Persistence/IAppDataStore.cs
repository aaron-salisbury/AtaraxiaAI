using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Persistence
{
    public interface IAppDataStore
    {
        Task<AppData> ReadAppDataAsync();
        Task SaveAppDataAsync(AppData data);
        void DeleteAppData();
    }
}
