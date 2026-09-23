using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Persistence
{
    public interface IAppDataStore
    {
        Task<InternalStorage> ReadInternalStorageAsync();
        Task<InternalStorage> UpdateInternalStorageAsync(InternalStorage storage, string newDirectory);
        Task<AppData> ReadAppDataAsync(string directory);
        Task SaveAppDataAsync(AppData data, string directory);
        void DeleteAppData(string directory);
    }
}
