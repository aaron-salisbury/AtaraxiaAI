using AtaraxiaAI.Business.Persistence;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AtaraxiaAI.Data
{
    // Persists AtaraxiaAI.json in the user's local application data folder.
    public sealed class JsonAppDataStore : IAppDataStore
    {
        private const string AppFile = "AtaraxiaAI.json";
        private readonly string _appFilePath;

        public static string DefaultDataDirectory { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AtaraxiaAI");

        public JsonAppDataStore() : this(DefaultDataDirectory) { }

        public JsonAppDataStore(string dataDirectory)
        {
            _appFilePath = Path.Combine(dataDirectory, AppFile);
        }

        public Task<AppData> ReadAppDataAsync() => ReadAsync<AppData>(_appFilePath);

        public Task SaveAppDataAsync(AppData data) => WriteAsync(_appFilePath, data);

        public void DeleteAppData()
        {
            if (File.Exists(_appFilePath)) File.Delete(_appFilePath);
        }

        private static async Task<T> ReadAsync<T>(string path)
        {
            if (!File.Exists(path)) return default;
            using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream);
        }

        private static async Task WriteAsync<T>(string path, T data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temporary = path + ".tmp";
            try
            {
                using (var stream = File.Create(temporary))
                    await JsonSerializer.SerializeAsync(stream, data);
                File.Move(temporary, path, true);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }
}
