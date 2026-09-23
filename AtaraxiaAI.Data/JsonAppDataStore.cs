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

        public async Task<InternalStorage> ReadInternalStorageAsync()
        {
            var storage = await ReadAsync<InternalStorage>(StorageFile);
            if (storage != null) return storage;

            storage = new InternalStorage
            {
                UserStorageDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AtaraxiaAI")
            };
            await WriteAsync(StorageFile, storage);
            return storage;
        }

        public async Task<InternalStorage> UpdateInternalStorageAsync(InternalStorage storage, string newDirectory)
        {
            if (string.IsNullOrWhiteSpace(newDirectory))
                throw new ArgumentException("A storage directory is required.", nameof(newDirectory));

            Directory.CreateDirectory(newDirectory);
            string source = Path.Combine(storage.UserStorageDirectory, AppFile);
            string destination = Path.Combine(newDirectory, AppFile);
            if (File.Exists(source) && !File.Exists(destination))
                File.Move(source, destination);

            storage = new InternalStorage { UserStorageDirectory = newDirectory };
            await WriteAsync(StorageFile, storage);
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
