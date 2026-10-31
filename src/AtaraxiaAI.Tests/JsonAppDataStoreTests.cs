using AtaraxiaAI.Business.Persistence;
using AtaraxiaAI.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
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
    public async Task ReadsNothingBeforeFirstSave()
    {
        var store = new JsonAppDataStore(Path.Combine(_root, "data"));

        Assert.IsNull(await store.ReadAppDataAsync());
    }

    [TestMethod]
    public async Task SavedDataRoundTrips()
    {
        string directory = Path.Combine(_root, "data");
        var store = new JsonAppDataStore(directory);
        var resetsOn = new DateTime(2026, 11, 15);

        await store.SaveAppDataAsync(new AppData { WatchmodeCurrentAPIUsage = 42, WatchmodeQuotaResetsOn = resetsOn });
        var data = await new JsonAppDataStore(directory).ReadAppDataAsync();

        Assert.AreEqual(42, data.WatchmodeCurrentAPIUsage);
        Assert.AreEqual(resetsOn, data.WatchmodeQuotaResetsOn);
        Assert.IsTrue(File.Exists(Path.Combine(directory, "AtaraxiaAI.json")));
    }

    [TestMethod]
    public async Task DeleteRemovesSavedData()
    {
        var store = new JsonAppDataStore(Path.Combine(_root, "data"));
        await store.SaveAppDataAsync(new AppData { WatchmodeCurrentAPIUsage = 1 });

        store.DeleteAppData();

        Assert.IsNull(await store.ReadAppDataAsync());
    }
}
