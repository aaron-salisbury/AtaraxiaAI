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
    public async Task ReadsExistingFileNamesAndProperties()
    {
        string directory = Path.Combine(_root, "saved");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(_root, "InternalStorage.json"),
            JsonSerializer.Serialize(new InternalStorage { UserStorageDirectory = directory }));
        await File.WriteAllTextAsync(Path.Combine(directory, "AtaraxiaAI.json"),
            "{\"MonthOfLastCloudServicesRoll\":7,\"WatchmodeCurrentAPIUsage\":42}");

        var store = new JsonAppDataStore(_root);
        var storage = await store.ReadInternalStorageAsync();
        var data = await store.ReadAppDataAsync(storage.UserStorageDirectory);

        Assert.AreEqual(directory, storage.UserStorageDirectory);
        Assert.AreEqual(7, data.MonthOfLastCloudServicesRoll);
        Assert.AreEqual(42, data.WatchmodeCurrentAPIUsage);
    }

    [TestMethod]
    public async Task MovingStoragePreservesAnExistingDestinationFile()
    {
        string oldDirectory = Path.Combine(_root, "old");
        string newDirectory = Path.Combine(_root, "new");
        var store = new JsonAppDataStore(_root);
        await store.SaveAppDataAsync(new AppData { WatchmodeCurrentAPIUsage = 1 }, oldDirectory);
        await store.SaveAppDataAsync(new AppData { WatchmodeCurrentAPIUsage = 17 }, newDirectory);

        var moved = await store.UpdateInternalStorageAsync(
            new InternalStorage { UserStorageDirectory = oldDirectory }, newDirectory);

        Assert.AreEqual(newDirectory, moved.UserStorageDirectory);
        Assert.AreEqual(17, (await store.ReadAppDataAsync(newDirectory)).WatchmodeCurrentAPIUsage);
        Assert.AreEqual(1, (await store.ReadAppDataAsync(oldDirectory)).WatchmodeCurrentAPIUsage);
        Assert.AreEqual(newDirectory, (await store.ReadInternalStorageAsync()).UserStorageDirectory);
    }

    [TestMethod]
    public async Task MovesAppDataWhenDestinationIsEmpty()
    {
        string oldDirectory = Path.Combine(_root, "old");
        string newDirectory = Path.Combine(_root, "new");
        var store = new JsonAppDataStore(_root);
        await store.SaveAppDataAsync(new AppData { WatchmodeCurrentAPIUsage = 9 }, oldDirectory);

        await store.UpdateInternalStorageAsync(
            new InternalStorage { UserStorageDirectory = oldDirectory }, newDirectory);

        Assert.IsFalse(File.Exists(Path.Combine(oldDirectory, "AtaraxiaAI.json")));
        Assert.AreEqual(9, (await store.ReadAppDataAsync(newDirectory)).WatchmodeCurrentAPIUsage);
    }
}
