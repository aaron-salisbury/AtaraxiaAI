using AtaraxiaAI.Business.Persistence;
using AtaraxiaAI.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AtaraxiaAI.Tests;

[TestClass]
public sealed class JsonAppDataStoreTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "AtaraxiaAI.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, recursive: true);

    [TestMethod]
    public async Task MigratesLegacyStorageToDefaultWithoutDeletingOriginal()
    {
        string oldDirectory = Path.Combine(_root, "saved");
        string defaultDirectory = Path.Combine(_root, "default");
        Directory.CreateDirectory(oldDirectory);
        await File.WriteAllTextAsync(Path.Combine(_root, "InternalStorage.json"),
            JsonSerializer.Serialize(new InternalStorage { UserStorageDirectory = oldDirectory }));
        await File.WriteAllTextAsync(Path.Combine(oldDirectory, "AtaraxiaAI.json"),
            "{\"WatchmodeCurrentAPIUsage\":42}");

        var store = new JsonAppDataStore(_root, defaultDirectory);
        var storage = await store.ReadInternalStorageAsync();

        Assert.AreEqual(defaultDirectory, storage.UserStorageDirectory);
        Assert.AreEqual(42, (await store.ReadAppDataAsync(defaultDirectory)).WatchmodeCurrentAPIUsage);
        Assert.IsTrue(File.Exists(Path.Combine(oldDirectory, "AtaraxiaAI.json")));
        Assert.AreEqual(defaultDirectory, (await store.ReadInternalStorageAsync()).UserStorageDirectory);
    }

    [TestMethod]
    public async Task MigrationPreservesExistingDefaultData()
    {
        string oldDirectory = Path.Combine(_root, "saved");
        string defaultDirectory = Path.Combine(_root, "default");
        var store = new JsonAppDataStore(_root, defaultDirectory);
        await store.SaveAppDataAsync(new AppData { WatchmodeCurrentAPIUsage = 1 }, oldDirectory);
        await store.SaveAppDataAsync(new AppData { WatchmodeCurrentAPIUsage = 17 }, defaultDirectory);
        await File.WriteAllTextAsync(Path.Combine(_root, "InternalStorage.json"),
            JsonSerializer.Serialize(new InternalStorage { UserStorageDirectory = oldDirectory }));

        await store.ReadInternalStorageAsync();

        Assert.AreEqual(17, (await store.ReadAppDataAsync(defaultDirectory)).WatchmodeCurrentAPIUsage);
    }
}
