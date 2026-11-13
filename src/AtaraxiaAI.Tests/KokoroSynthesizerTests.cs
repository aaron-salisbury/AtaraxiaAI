using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Integrations.Services;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using Microsoft.ML.OnnxRuntime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Tests;

[TestClass]
public sealed class KokoroSynthesizerTests
{
    private static readonly byte[] MODEL = Encoding.UTF8.GetBytes("fake model for download tests");
    private static readonly KokoroDownload DOWNLOAD = new("https://speech.example/kokoro.onnx", MODEL.Length, Convert.ToHexString(SHA256.HashData(MODEL)));

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Prepare_DownloadsAndVerifiesTheModelOnceReportingProgress()
    {
        using TestFolder folder = new();
        using Server server = new(MODEL);
        using KokoroSynthesizer kokoro = Create(folder.Path, server);
        List<double> progress = [];
        Assert.IsFalse(kokoro.IsAvailable());

        Assert.IsTrue(await kokoro.PrepareAsync(new ImmediateProgress(progress.Add), TestContext.CancellationToken));
        Assert.IsTrue(await kokoro.PrepareAsync(null, TestContext.CancellationToken));
        using KokoroSynthesizer reopened = Create(folder.Path, server);

        Assert.IsTrue(kokoro.IsAvailable());
        Assert.IsTrue(reopened.IsAvailable());
        Assert.AreEqual(1, server.Requests);
        Assert.AreEqual(0d, progress[0]);
        Assert.AreEqual(100d, progress[^1]);
    }

    [TestMethod]
    public async Task Prepare_CorruptOrFailedDownloadsAreNeverAvailableAndCanBeRetried()
    {
        using TestFolder folder = new();
        using Server server = new([.. Enumerable.Repeat((byte)'x', MODEL.Length)]);
        using KokoroSynthesizer kokoro = Create(folder.Path, server);

        Assert.IsFalse(await kokoro.PrepareAsync(null, TestContext.CancellationToken));
        Assert.IsFalse(kokoro.IsAvailable());
        Assert.IsFalse(File.Exists(Path.Combine(folder.Path, "kokoro.onnx.download")));
        server.Status = HttpStatusCode.NotFound;
        Assert.IsFalse(await kokoro.PrepareAsync(null, TestContext.CancellationToken));
        server.Status = HttpStatusCode.OK;
        server.Content = MODEL;
        Assert.IsTrue(await kokoro.PrepareAsync(null, TestContext.CancellationToken));
        Assert.IsTrue(kokoro.IsAvailable());
    }

    [TestMethod]
    public async Task Prepare_CancellationReleasesTheGateAndDoesNotInstallAPartialModel()
    {
        using TestFolder folder = new();
        using Server server = new(MODEL) { Hold = true };
        using KokoroSynthesizer kokoro = Create(folder.Path, server);
        using CancellationTokenSource canceled = new();

        Task<bool> downloading = kokoro.PrepareAsync(null, canceled.Token);
        await server.Started.Task.WaitAsync(TestContext.CancellationToken);
        canceled.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => downloading);
        Assert.IsFalse(kokoro.IsAvailable());
        server.Hold = false;
        Assert.IsTrue(await kokoro.PrepareAsync(null, TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task Prepare_ResumesAfterAConnectionDropsInsteadOfStartingOver()
    {
        using TestFolder folder = new();
        using Server server = new(MODEL) { DropAfter = 10 };
        using KokoroSynthesizer kokoro = Create(folder.Path, server);

        bool dropped = await kokoro.PrepareAsync(null, TestContext.CancellationToken);
        long kept = new FileInfo(Path.Combine(folder.Path, "kokoro.onnx.download")).Length;
        server.DropAfter = null;
        bool resumed = await kokoro.PrepareAsync(null, TestContext.CancellationToken);

        Assert.IsFalse(dropped);
        Assert.AreEqual(10, kept);
        Assert.AreEqual(10, server.LastRangeStart);
        Assert.IsTrue(resumed);
        Assert.IsTrue(kokoro.IsAvailable());
    }

    [TestMethod]
    public async Task Prepare_AnOversizedDownloadFails()
    {
        using TestFolder folder = new();
        using Server server = new([.. MODEL, .. MODEL]);
        using KokoroSynthesizer kokoro = Create(folder.Path, server);

        Assert.IsFalse(await kokoro.PrepareAsync(null, TestContext.CancellationToken));
        Assert.IsFalse(kokoro.IsAvailable());
    }

    [TestMethod]
    public async Task Synthesize_FailsBeforeTheModelIsDownloaded()
    {
        using TestFolder folder = new();
        using Server server = new(MODEL);
        using KokoroSynthesizer kokoro = Create(folder.Path, server);

        await Assert.ThrowsAsync<InvalidOperationException>(() => kokoro.SynthesizeAsync("Hello", null, TestContext.CancellationToken));
        Assert.AreEqual(0, server.Requests);
    }

    [TestMethod]
    public void PrepareText_ReplacesSymbolsThePhonemizerCannotHandle()
    {
        string emoji = char.ConvertFromUtf32(0x1F642);
        // An emoji, a lone surrogate, a control character, and a noncharacter.
        string unspeakable = emoji + new string([(char)0xD800, (char)0, (char)0xFFFE]);

        Assert.AreEqual("Hi   there", KokoroSynthesizer.PrepareText("Hi " + emoji + " there"));
        Assert.IsTrue(string.IsNullOrWhiteSpace(KokoroSynthesizer.PrepareText(unspeakable)));
    }

    [TestMethod]
    public void ToWav_WrapsPcmAs24KilohertzMono16Bit()
    {
        byte[] pcm = new byte[4800];

        using WaveFileReader reader = new(new MemoryStream(KokoroSynthesizer.ToWav(pcm)));

        Assert.AreEqual(24000, reader.WaveFormat.SampleRate);
        Assert.AreEqual(1, reader.WaveFormat.Channels);
        Assert.AreEqual(16, reader.WaveFormat.BitsPerSample);
        Assert.AreEqual(pcm.Length, reader.Length);
    }

    [TestMethod]
    public void Segments_SplitALongMessageWithinTheModelLimit()
    {
        int[] tokens = Tokenizer.Tokenize(string.Concat(Enumerable.Repeat("Hello there, how are you doing today? I hope the garden is well. ", 40)), "en-us");

        int[][] segments = [.. KokoroSynthesizer.Segments(tokens)];

        Assert.IsGreaterThan(1, segments.Length);
        Assert.IsTrue(segments.All(segment => segment.Length <= KokoroModel.maxTokens));
    }

    /// <summary>Kokoro runs in this process, so its native ONNX runtime, voices, and phonemizer must load here.</summary>
    [TestMethod]
    public void TheOnnxRuntimeVoiceAndPhonemizerLoadInProcess()
    {
        using SessionOptions options = new();
        Assert.Contains("CPUExecutionProvider", OrtEnv.Instance().GetAvailableProviders());

        KokoroVoice voice = KokoroVoice.FromPath(Path.Combine(AppContext.BaseDirectory, "SpeechVoices", "af_heart.npy"));

        Assert.AreEqual(256, voice.Features.GetLength(2));
        Assert.IsNotEmpty(Tokenizer.Tokenize("Hello, how are you today?", "en-us"));
        Assert.IsNotEmpty(Tokenizer.Tokenize("Hello, how are you today?", "en-gb"));
    }

    [TestMethod]
    public void Voices_EveryListedVoiceIsBundledWithHeartAsTheDefault()
    {
        using TestFolder folder = new();
        using Server server = new(MODEL);
        using KokoroSynthesizer kokoro = Create(folder.Path, server);

        Assert.AreEqual("af_heart", kokoro.Voices[0].Id);
        Assert.HasCount(28, kokoro.Voices);
        Assert.AreEqual(13, kokoro.Voices.Count(voice => voice.Gender == VoiceGender.Male));
        foreach (SpeechVoice listed in kokoro.Voices)
        {
            KokoroVoice data = KokoroVoice.FromPath(Path.Combine(AppContext.BaseDirectory, "SpeechVoices", listed.Id + ".npy"));
            Assert.AreEqual(256, data.Features.GetLength(2), listed.Id);
        }
    }

    /// <summary>No network or audio device is needed. Set ATARAXIA_KOKORO_TEST_MODEL to a downloaded kokoro.onnx.</summary>
    [TestMethod]
    [TestCategory("LocalSpeech")]
    public async Task RealKokoro_SynthesizesAPlayableWav()
    {
        string? modelPath = Environment.GetEnvironmentVariable("ATARAXIA_KOKORO_TEST_MODEL");
        if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
        {
            Assert.Inconclusive("Set ATARAXIA_KOKORO_TEST_MODEL to a downloaded kokoro.onnx to test real synthesis.");
        }

        using TestFolder folder = new();
        File.Copy(modelPath, Path.Combine(folder.Path, "kokoro.onnx"));
        using Server server = new(MODEL);
        using KokoroSynthesizer kokoro = new(folder.Path, server, TestDependencies.Logger);
        string message = "Hello " + char.ConvertFromUtf32(0x1F642) + ", how are you today?";

        // An American voice, a British voice, and an unknown ID, which falls back to the default voice.
        foreach (string voiceId in new[] { "af_heart", "bm_george", "../not-a-voice" })
        {
            byte[] wav = await kokoro.SynthesizeAsync(message, voiceId, TestContext.CancellationToken);

            using WaveFileReader reader = new(new MemoryStream(wav));
            Assert.IsGreaterThan(TimeSpan.FromSeconds(0.5), reader.TotalTime, voiceId);
        }

        Assert.AreEqual(0, server.Requests);
    }

    private static KokoroSynthesizer Create(string directory, Server server)
    {
        return new(directory, server, TestDependencies.Logger, DOWNLOAD);
    }

    private sealed class ImmediateProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value)
        {
            report(value);
        }
    }

    /// <remarks>Adapted from HomiliaMessenger's Kokoro speech tests.</remarks>
    private sealed class Server : HttpMessageHandler, IHttpClientFactory
    {
        public Server(byte[] content)
        {
            Content = content;
        }

        public byte[] Content { get; set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public bool Hold { get; set; }
        public int? DropAfter { get; set; }
        public long? LastRangeStart { get; private set; }
        public int Requests { get; private set; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public HttpClient CreateClient(string name)
        {
            return new(this, disposeHandler: false);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Started.TrySetResult();
            if (Hold)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            if (Status != HttpStatusCode.OK)
            {
                return new(Status);
            }

            long start = request.Headers.Range?.Ranges.First().From ?? 0;
            LastRangeStart = start > 0 ? start : LastRangeStart;
            byte[] body = Content[(int)start..];
            HttpContent content = DropAfter is { } drop ? new StreamContent(new DroppingStream(body, drop)) : new ByteArrayContent(body);
            return new(start > 0 ? HttpStatusCode.PartialContent : HttpStatusCode.OK) { Content = content };
        }
    }

    /// <summary>Sends the first bytes, then fails as a dropped connection does.</summary>
    private sealed class DroppingStream(byte[] content, int dropAfter) : MemoryStream(content)
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Position >= dropAfter)
            {
                throw new IOException("The connection was reset.");
            }

            return base.Read(buffer, offset, (int)Math.Min(count, dropAfter - Position));
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.FromResult(Read(buffer, offset, count));
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            byte[] chunk = new byte[buffer.Length];
            int read = Read(chunk, 0, chunk.Length);
            chunk.AsSpan(0, read).CopyTo(buffer.Span);
            return ValueTask.FromResult(read);
        }
    }

    private sealed class TestFolder : IDisposable
    {
        public TestFolder()
        {
            Directory.CreateDirectory(Path);
        }

        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AtaraxiaKokoro-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
