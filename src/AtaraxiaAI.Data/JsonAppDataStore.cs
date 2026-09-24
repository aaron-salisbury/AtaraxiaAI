using AtaraxiaAI.Business.Persistence;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AtaraxiaAI.Data
{
    // Keeps the existing AtaraxiaAI.json and InternalStorage.json format and locations.
    public sealed class JsonAppDataStore : IAppDataStore
    {
        private const string AppFile = "AtaraxiaAI.json";
        private const string StorageFile = "InternalStorage.json";
        private readonly string _storageFilePath;
        private readonly string _dataDirectory;

        public JsonAppDataStore() : this(".") { }

        public JsonAppDataStore(string internalDirectory) : this(internalDirectory,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AtaraxiaAI")) { }

        public JsonAppDataStore(string internalDirectory, string dataDirectory)
        {
            _storageFilePath = Path.Combine(internalDirectory, StorageFile);
            _dataDirectory = dataDirectory;
        }

        public async Task<InternalStorage> ReadInternalStorageAsync()
        {
            string directory = _dataDirectory;
            var previous = await ReadAsync<InternalStorage>(_storageFilePath);
            string destination = Path.Combine(directory, AppFile);
            if (previous?.UserStorageDirectory is { Length: > 0 } oldDirectory &&
                !Path.GetFullPath(oldDirectory).Equals(Path.GetFullPath(directory), StringComparison.OrdinalIgnoreCase) &&
                !File.Exists(destination))
            {
                string source = Path.Combine(oldDirectory, AppFile);
                if (File.Exists(source))
                {
                    Directory.CreateDirectory(directory);
                    File.Copy(source, destination, overwrite: false);
                }
            }

            var storage = new InternalStorage { UserStorageDirectory = directory };
            if (previous?.UserStorageDirectory != directory)
                await WriteAsync(_storageFilePath, storage);
            return storage;
        }

        public Task<AppData> ReadAppDataAsync(string directory) =>
            ReadAsync<AppData>(Path.Combine(directory, AppFile));

        public Task SaveAppDataAsync(AppData data, string directory) =>
            WriteAsync(Path.Combine(directory, AppFile), data);

        public void DeleteAppData(string directory)
        {
            string path = Path.Combine(directory, AppFile);
            if (File.Exists(path)) File.Delete(path);
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
