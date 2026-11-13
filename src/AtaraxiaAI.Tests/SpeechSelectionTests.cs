using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Tests;

[TestClass]
public sealed class SpeechSelectionTests
{
    private static readonly SpeechVoice HEART = new("af_heart", "Heart (American)", VoiceGender.Female);
    private static readonly SpeechVoice ADAM = new("am_adam", "Adam (American)", VoiceGender.Male);

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void UsersChooseSystemOrKokoroInThatOrder()
    {
        // The System default is covered by the desktop settings and settings view model tests.
        CollectionAssert.AreEqual(
            new[] { SpeechSynthesizers.SystemDotSpeech, SpeechSynthesizers.Kokoro },
            new List<SpeechSynthesizers>(SpeechEngine.SelectableSynthesizers));
    }

    [TestMethod]
    public async Task SelectingASourcePreparesItAndPassesDownloadProgressThrough()
    {
        FakeIntegrationFactory factory = new();
        List<double> reported = [];
        factory.Synthesizers[SpeechSynthesizers.Kokoro].Prepare = (progress, _) =>
        {
            progress?.Report(50);
            progress?.Report(100);
            return Task.FromResult(true);
        };
        SpeechEngine engine = Create(factory);

        bool selected = await engine.SelectSynthesizerAsync(SpeechSynthesizers.Kokoro, new ImmediateProgress(reported.Add), TestContext.CancellationToken);

        Assert.IsTrue(selected);
        Assert.AreEqual(SpeechSynthesizers.Kokoro, engine.ActiveSynthesizer);
        CollectionAssert.AreEqual(new[] { 50d, 100d }, reported);
    }

    [TestMethod]
    public async Task ASourceThatCannotBePreparedLeavesTheCurrentSourceInUse()
    {
        FakeIntegrationFactory factory = new();
        factory.Synthesizers[SpeechSynthesizers.SystemDotSpeech].Prepare = (_, _) => Task.FromResult(false);
        SpeechEngine engine = Create(factory);
        await engine.SelectSynthesizerAsync(SpeechSynthesizers.Kokoro, cancellationToken: TestContext.CancellationToken);

        bool selected = await engine.SelectSynthesizerAsync(SpeechSynthesizers.SystemDotSpeech, cancellationToken: TestContext.CancellationToken);

        Assert.IsFalse(selected);
        Assert.AreEqual(SpeechSynthesizers.Kokoro, engine.ActiveSynthesizer);
    }

    [TestMethod]
    public async Task WithNoSourceInUseAnUnavailableSelectionFallsBackToSystemSpeech()
    {
        FakeIntegrationFactory factory = new();
        factory.Synthesizers[SpeechSynthesizers.Kokoro].Prepare = (_, _) => throw new InvalidOperationException("offline");
        SpeechEngine engine = Create(factory);

        bool selected = await engine.SelectSynthesizerAsync(SpeechSynthesizers.Kokoro, cancellationToken: TestContext.CancellationToken);

        Assert.IsFalse(selected);
        Assert.AreEqual(SpeechSynthesizers.SystemDotSpeech, engine.ActiveSynthesizer);
    }

    [TestMethod]
    public async Task ACanceledSelectionThrowsAndLeavesTheCurrentSourceInUse()
    {
        FakeIntegrationFactory factory = new();
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Synthesizers[SpeechSynthesizers.Kokoro].Prepare = async (_, token) =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
            return true;
        };
        SpeechEngine engine = Create(factory);
        await engine.SelectSynthesizerAsync(SpeechSynthesizers.SystemDotSpeech, cancellationToken: TestContext.CancellationToken);
        using CancellationTokenSource cancel = new();

        Task<bool> selecting = engine.SelectSynthesizerAsync(SpeechSynthesizers.Kokoro, cancellationToken: cancel.Token);
        await started.Task.WaitAsync(TestContext.CancellationToken);
        cancel.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => selecting);
        Assert.AreEqual(SpeechSynthesizers.SystemDotSpeech, engine.ActiveSynthesizer);
        // The selection gate was released, so another selection proceeds.
        factory.Synthesizers[SpeechSynthesizers.Kokoro].Prepare = (_, _) => Task.FromResult(true);
        Assert.IsTrue(await engine.SelectSynthesizerAsync(SpeechSynthesizers.Kokoro, cancellationToken: TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task SelectingTheSourceInUseDoesNotPrepareItAgain()
    {
        FakeIntegrationFactory factory = new();
        SpeechEngine engine = Create(factory);
        await engine.SelectSynthesizerAsync(SpeechSynthesizers.Kokoro, cancellationToken: TestContext.CancellationToken);

        Assert.IsTrue(await engine.SelectSynthesizerAsync(SpeechSynthesizers.Kokoro, cancellationToken: TestContext.CancellationToken));

        Assert.AreEqual(1, factory.Synthesizers[SpeechSynthesizers.Kokoro].PrepareCalls);
    }

    [TestMethod]
    public async Task SpeechThatFailsWithTheSelectedSourceIsSpokenWithSystemSpeech()
    {
        FakeIntegrationFactory factory = new();
        factory.Synthesizers[SpeechSynthesizers.Kokoro].Synthesize = (_, _) => throw new InvalidOperationException("model failed");
        SpeechEngine engine = Create(factory);
        await engine.SelectSynthesizerAsync(SpeechSynthesizers.Kokoro, cancellationToken: TestContext.CancellationToken);

        await engine.SpeakAsync("Hello", TestContext.CancellationToken);

        CollectionAssert.AreEqual(new[] { "Hello" }, factory.Synthesizers[SpeechSynthesizers.SystemDotSpeech].Spoken);
        Assert.AreEqual(SpeechSynthesizers.SystemDotSpeech, engine.ActiveSynthesizer);
    }

    [TestMethod]
    public async Task SpeechUsesTheChosenVoiceAndTheActiveSourceListsItsVoices()
    {
        FakeIntegrationFactory factory = new();
        FakeSynthesizer kokoro = factory.Synthesizers[SpeechSynthesizers.Kokoro];
        kokoro.Voices = [HEART, ADAM];
        SpeechEngine engine = Create(factory);
        await engine.SelectSynthesizerAsync(SpeechSynthesizers.Kokoro, cancellationToken: TestContext.CancellationToken);

        engine.VoiceId = ADAM.Id;
        await engine.SpeakAsync("Hello", TestContext.CancellationToken);

        CollectionAssert.AreEqual(new[] { HEART, ADAM }, new List<SpeechVoice>(engine.ActiveVoices));
        CollectionAssert.AreEqual(new[] { ADAM.Id }, kokoro.SpokenVoices);
    }

    [TestMethod]
    public async Task AVoiceSampleIsSpokenOnlyWithAVoiceTheActiveSourceOffers()
    {
        FakeIntegrationFactory factory = new();
        FakeSynthesizer kokoro = factory.Synthesizers[SpeechSynthesizers.Kokoro];
        kokoro.Voices = [HEART, ADAM];
        List<byte[]> played = [];
        IAudioPlayer player = Proxy<IAudioPlayer>.Create((_, args) =>
        {
            played.Add((byte[])args![0]!);
            return Task.CompletedTask;
        });
        SpeechEngine engine = new(factory, player, TestDependencies.Create());
        await engine.SelectSynthesizerAsync(SpeechSynthesizers.Kokoro, cancellationToken: TestContext.CancellationToken);

        await engine.PlayVoiceSampleAsync(ADAM.Id, TestContext.CancellationToken);
        await engine.PlayVoiceSampleAsync("../not-a-voice", TestContext.CancellationToken);

        Assert.HasCount(1, played);
        CollectionAssert.AreEqual(new[] { ADAM.Id }, kokoro.SpokenVoices);
    }

    [TestMethod]
    public void UsersChooseSystemOrVoskRecognitionInThatOrder()
    {
        CollectionAssert.AreEqual(
            new[] { SpeechRecognizers.SystemDotSpeech, SpeechRecognizers.Vosk },
            new List<SpeechRecognizers>(SpeechEngine.SelectableRecognizers));
    }

    [TestMethod]
    public async Task SelectingARecognizerWhileListeningPreparesItAndKeepsListeningWithIt()
    {
        FakeIntegrationFactory factory = new();
        List<double> reported = [];
        factory.Recognizers[SpeechRecognizers.Vosk].Prepare = (progress, _) =>
        {
            progress?.Report(100);
            return Task.FromResult(true);
        };
        SpeechEngine engine = Create(factory);
        Assert.IsTrue(engine.ActivateSpeechRecognition());

        bool selected = await engine.SelectRecognizerAsync(SpeechRecognizers.Vosk, new ImmediateProgress(reported.Add), TestContext.CancellationToken);

        Assert.IsTrue(selected);
        Assert.AreEqual(SpeechRecognizers.Vosk, engine.ActiveRecognizer);
        Assert.IsTrue(engine.IsSpeechRecognitionRunning);
        Assert.IsFalse(factory.Recognizers[SpeechRecognizers.SystemDotSpeech].IsListening);
        Assert.IsTrue(factory.Recognizers[SpeechRecognizers.Vosk].IsListening);
        CollectionAssert.AreEqual(new[] { 100d }, reported);
    }

    [TestMethod]
    public async Task ARecognizerThatCannotBePreparedLeavesTheCurrentOneListening()
    {
        FakeIntegrationFactory factory = new();
        factory.Recognizers[SpeechRecognizers.Vosk].Prepare = (_, _) => throw new InvalidOperationException("offline");
        SpeechEngine engine = Create(factory);
        engine.ActivateSpeechRecognition();

        bool selected = await engine.SelectRecognizerAsync(SpeechRecognizers.Vosk, cancellationToken: TestContext.CancellationToken);

        Assert.IsFalse(selected);
        Assert.AreEqual(SpeechRecognizers.SystemDotSpeech, engine.ActiveRecognizer);
        Assert.IsTrue(factory.Recognizers[SpeechRecognizers.SystemDotSpeech].IsListening);
        Assert.IsFalse(factory.Recognizers[SpeechRecognizers.Vosk].IsListening);
    }

    [TestMethod]
    public async Task ACanceledRecognizerSelectionThrowsAndLeavesTheCurrentOneInUse()
    {
        FakeIntegrationFactory factory = new();
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Recognizers[SpeechRecognizers.Vosk].Prepare = async (_, token) =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
            return true;
        };
        SpeechEngine engine = Create(factory);
        using CancellationTokenSource cancel = new();

        Task<bool> selecting = engine.SelectRecognizerAsync(SpeechRecognizers.Vosk, cancellationToken: cancel.Token);
        await started.Task.WaitAsync(TestContext.CancellationToken);
        cancel.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => selecting);
        Assert.AreEqual(SpeechRecognizers.SystemDotSpeech, engine.ActiveRecognizer);
    }

    [TestMethod]
    public async Task TheChosenCaptureSourceCarriesOverToANewRecognizer()
    {
        FakeIntegrationFactory factory = new();
        SpeechEngine engine = Create(factory);

        engine.UpdateCaptureSource(SoundCaptureSources.Microphone);
        await engine.SelectRecognizerAsync(SpeechRecognizers.Vosk, cancellationToken: TestContext.CancellationToken);

        Assert.AreEqual(SoundCaptureSources.Microphone, factory.Recognizers[SpeechRecognizers.SystemDotSpeech].CaptureSource);
        Assert.AreEqual(SoundCaptureSources.Microphone, factory.Recognizers[SpeechRecognizers.Vosk].CaptureSource);
    }

    [TestMethod]
    public void ListeningThatCannotStartIsReportedRatherThanThrown()
    {
        FakeIntegrationFactory factory = new();
        factory.Recognizer.ListenFailure = new InvalidOperationException("No audio input device.");
        SpeechEngine engine = Create(factory);

        Assert.IsFalse(engine.ActivateSpeechRecognition());
        Assert.IsFalse(engine.IsSpeechRecognitionRunning);
    }

    private static SpeechEngine Create(FakeIntegrationFactory factory)
    {
        return new SpeechEngine(factory, TestDependencies.SilentPlayer, TestDependencies.Create());
    }

    /// <summary>Reports synchronously, unlike <see cref="Progress{T}"/>, which posts to the thread pool.</summary>
    private sealed class ImmediateProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value)
        {
            report(value);
        }
    }
}
