using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Persistence
{
    public interface IAppDataStore
    {
        Task<InternalStorage> ReadInternalStorageAsync();
        Task<AppData> ReadAppDataAsync(string directory);
        Task SaveAppDataAsync(AppData data, string directory);
        void DeleteAppData(string directory);
    }
}
