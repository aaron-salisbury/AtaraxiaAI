using AtaraxiaAI.Integrations.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Tests;

[TestClass]
public sealed class VoskRecognizerTests
{
    private const string MODEL_NAME = "vosk-test-model";

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Install_DownloadsVerifiesAndExtractsTheModelOnceReportingProgress()
    {
        byte[] model = CreateModelZip(MODEL_NAME, VoskRecognizer.REQUIRED_FILES);
        using TestFolder folder = new();
        using FakeModelServer server = new(model);
        using VoskRecognizer vosk = Create(folder.Path, server, model);
        List<double> progress = [];

        Assert.IsTrue(await vosk.InstallAsync(new ImmediateProgress(progress.Add), TestContext.CancellationToken));
        Assert.IsTrue(await vosk.InstallAsync(null, TestContext.CancellationToken));
        using VoskRecognizer reopened = Create(folder.Path, server, model);

        Assert.IsTrue(vosk.IsInstalled);
        Assert.IsTrue(reopened.IsInstalled);
        Assert.AreEqual(1, server.Requests);
        Assert.AreEqual(0d, progress[0]);
        Assert.AreEqual(100d, progress[^1]);
        // Only the extracted model is kept; the download and the staging folder are removed.
        CollectionAssert.AreEquivalent(new[] { MODEL_NAME }, Directory.GetFileSystemEntries(folder.Path).Select(Path.GetFileName).ToArray());
    }

    [TestMethod]
    public async Task Install_ACorruptDownloadIsDiscardedAndCanBeRetried()
    {
        byte[] model = CreateModelZip(MODEL_NAME, VoskRecognizer.REQUIRED_FILES);
        using TestFolder folder = new();
        using FakeModelServer server = new([.. Enumerable.Repeat((byte)'x', model.Length)]);
        using VoskRecognizer vosk = Create(folder.Path, server, model);

        Assert.IsFalse(await vosk.InstallAsync(null, TestContext.CancellationToken));
        Assert.IsFalse(vosk.IsInstalled);
        Assert.IsEmpty(Directory.GetFileSystemEntries(folder.Path));
        server.Status = HttpStatusCode.NotFound;
        Assert.IsFalse(await vosk.InstallAsync(null, TestContext.CancellationToken));
        server.Status = HttpStatusCode.OK;
        server.Content = model;
        Assert.IsTrue(await vosk.InstallAsync(null, TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task Install_ADownloadWithoutTheModelFilesIsNotInstalled()
    {
        byte[] model = CreateModelZip("some-other-model", VoskRecognizer.REQUIRED_FILES);
        using TestFolder folder = new();
        using FakeModelServer server = new(model);
        using VoskRecognizer vosk = Create(folder.Path, server, model);

        Assert.IsFalse(await vosk.InstallAsync(null, TestContext.CancellationToken));

        Assert.IsFalse(vosk.IsInstalled);
        Assert.IsFalse(Directory.Exists(vosk.ModelPath + ".extracting"));
    }

    [TestMethod]
    public async Task Install_ResumesAfterAConnectionDropsInsteadOfStartingOver()
    {
        byte[] model = CreateModelZip(MODEL_NAME, VoskRecognizer.REQUIRED_FILES);
        using TestFolder folder = new();
        using FakeModelServer server = new(model) { DropAfter = 100 };
        using VoskRecognizer vosk = Create(folder.Path, server, model);

        bool dropped = await vosk.InstallAsync(null, TestContext.CancellationToken);
        server.DropAfter = null;
        bool resumed = await vosk.InstallAsync(null, TestContext.CancellationToken);

        Assert.IsFalse(dropped);
        Assert.AreEqual(100, server.LastRangeStart);
        Assert.IsTrue(resumed);
        Assert.IsTrue(vosk.IsInstalled);
    }

    [TestMethod]
    public async Task Install_CancellationReleasesTheGateAndInstallsNothing()
    {
        byte[] model = CreateModelZip(MODEL_NAME, VoskRecognizer.REQUIRED_FILES);
        using TestFolder folder = new();
        using FakeModelServer server = new(model) { Hold = true };
        using VoskRecognizer vosk = Create(folder.Path, server, model);
        using CancellationTokenSource canceled = new();

        Task<bool> installing = vosk.InstallAsync(null, canceled.Token);
        await server.Started.Task.WaitAsync(TestContext.CancellationToken);
        canceled.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => installing);
        Assert.IsFalse(vosk.IsInstalled);
        server.Hold = false;
        Assert.IsTrue(await vosk.InstallAsync(null, TestContext.CancellationToken));
    }

    [TestMethod]
    public void Listen_RequiresThePreparedModel()
    {
        using TestFolder folder = new();
        using FakeModelServer server = new([]);
        using VoskRecognizer vosk = new(folder.Path, server, TestDependencies.Logger);

        Assert.IsFalse(vosk.IsAvailable());
        Assert.ThrowsExactly<InvalidOperationException>(() => vosk.Listen(_ => { }));
        Assert.AreEqual(0, server.Requests);
    }

    [TestMethod]
    public void ToPhrase_IgnoresSilenceAndSingleWords()
    {
        Assert.AreEqual("hey robot tell me a joke", VoskRecognizer.ToPhrase("{ \"text\" : \"hey robot tell me a joke\" }"));
        Assert.IsNull(VoskRecognizer.ToPhrase("{ \"text\" : \"\" }"));
        Assert.IsNull(VoskRecognizer.ToPhrase("{ \"text\" : \"huh\" }"));
    }

    [TestMethod]
    public void AudioConverter_TurnsASoundCardMixIntoSixteenKilohertzMonoPcm()
    {
        // One second of 48 kHz stereo float, as WASAPI loopback capture commonly delivers it.
        WaveFormat mix = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        byte[] second = new byte[mix.AverageBytesPerSecond];
        VoskAudioConverter converter = new(mix);

        byte[] converted = converter.Convert(second, second.Length);

        // 16,000 samples of 2 bytes each, give or take the resampler's latency.
        Assert.IsTrue(converted.Length is > 30000 and <= 32000, $"Converted {converted.Length} bytes.");
        Assert.AreEqual(0, converted.Length % 2);
    }

    [TestMethod]
    public void AudioConverter_PassesRecognizerAudioThroughUnchanged()
    {
        VoskAudioConverter converter = new(new WaveFormat(VoskRecognizer.SAMPLE_RATE, 16, 1));
        byte[] audio = [1, 2, 3, 4, 5, 6];

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, converter.Convert(audio, 4));
    }

    /// <summary>No network or audio device is needed. Set ATARAXIA_VOSK_TEST_MODEL to an extracted vosk-model-en-us-0.22-lgraph folder.</summary>
    [TestMethod]
    [TestCategory("LocalSpeech")]
    public async Task RealVosk_TranscribesSpokenDigits()
    {
        string? modelPath = Environment.GetEnvironmentVariable("ATARAXIA_VOSK_TEST_MODEL");
        if (string.IsNullOrWhiteSpace(modelPath) || !Directory.Exists(modelPath))
        {
            Assert.Inconclusive("Set ATARAXIA_VOSK_TEST_MODEL to an extracted vosk-model-en-us-0.22-lgraph folder to test real recognition.");
        }

        using TestFolder folder = new();
        CopyDirectory(modelPath, Path.Combine(folder.Path, "vosk-model-en-us-0.22-lgraph"));
        using FakeModelServer server = new([]);
        using VoskRecognizer vosk = new(folder.Path, server, TestDependencies.Logger);

        Assert.IsTrue(await vosk.PrepareAsync(null, TestContext.CancellationToken));
        using WaveFileReader recording = new(Path.Combine(AppContext.BaseDirectory, "Assets", "vosk-digits.wav"));
        IReadOnlyList<string> phrases = vosk.Transcribe(recording);

        Assert.AreEqual(0, server.Requests);
        CollectionAssert.AreEqual(new[] { "one zero zero zero one", "nine oh two one oh", "zero one eight zero three" }, phrases.ToArray());
    }

    /// <summary>
    /// Plays the spoken digits aloud while Vosk listens to the sound card, exercising real loopback capture, format
    /// conversion, and streaming recognition. Set ATARAXIA_VOSK_TEST_MODEL and ATARAXIA_AUDIO_DEVICE_TESTS=1 to run it.
    /// </summary>
    [TestMethod]
    [TestCategory("AudioDevice")]
    public async Task RealVosk_HearsWhatTheSoundCardPlays()
    {
        string? modelPath = Environment.GetEnvironmentVariable("ATARAXIA_VOSK_TEST_MODEL");
        if (string.IsNullOrWhiteSpace(modelPath) || !Directory.Exists(modelPath) || Environment.GetEnvironmentVariable("ATARAXIA_AUDIO_DEVICE_TESTS") != "1")
        {
            Assert.Inconclusive("Set ATARAXIA_VOSK_TEST_MODEL and ATARAXIA_AUDIO_DEVICE_TESTS=1 to play audio through the sound card.");
        }

        using TestFolder folder = new();
        CopyDirectory(modelPath, Path.Combine(folder.Path, "vosk-model-en-us-0.22-lgraph"));
        using FakeModelServer server = new([]);
        using VoskRecognizer vosk = new(folder.Path, server, TestDependencies.Logger);
        Assert.IsTrue(await vosk.PrepareAsync(null, TestContext.CancellationToken));
        List<string> heard = [];

        vosk.UpdateCaptureSource(AtaraxiaAI.Business.Base.Enums.SoundCaptureSources.SoundCard);
        vosk.Listen(phrase =>
        {
            lock (heard)
            {
                heard.Add(phrase);
            }
        });

        try
        {
            using AudioFileReader recording = new(Path.Combine(AppContext.BaseDirectory, "Assets", "vosk-digits.wav"));
            using WaveOutEvent output = new();
            TaskCompletionSource played = new(TaskCreationOptions.RunContinuationsAsynchronously);
            output.PlaybackStopped += (_, _) => played.TrySetResult();
            output.Init(recording);
            output.Play();
            await played.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.CancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(2), TestContext.CancellationToken);
        }
        finally
        {
            vosk.Stop();
        }

        string transcript = string.Join(" | ", heard);
        Assert.Contains("one zero zero zero one", transcript, $"Heard: {transcript}");
        Assert.Contains("nine oh two one oh", transcript, $"Heard: {transcript}");
        // The last phrase ends only through the silence fed once the sound card goes quiet.
        Assert.Contains("zero one eight zero three", transcript, $"Heard: {transcript}");
    }

    private static VoskRecognizer Create(string directory, FakeModelServer server, byte[] model)
    {
        VoskModelDownload download = new(MODEL_NAME, "https://models.example/vosk.zip", model.Length, Convert.ToHexString(SHA256.HashData(model)), 1024);
        return new VoskRecognizer(directory, server, TestDependencies.Logger, download);
    }

    /// <summary>A zip holding placeholder model files under <paramref name="root"/>, as Vosk's model zips do.</summary>
    private static byte[] CreateModelZip(string root, IEnumerable<string> files)
    {
        using MemoryStream zip = new();
        using (ZipArchive archive = new(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (string file in files)
            {
                using StreamWriter writer = new(archive.CreateEntry($"{root}/{file}").Open());
                writer.Write("placeholder for " + file);
            }
        }

        return zip.ToArray();
    }

    private static void CopyDirectory(string source, string target)
    {
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
    }

    private sealed class ImmediateProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value)
        {
            report(value);
        }
    }
}
