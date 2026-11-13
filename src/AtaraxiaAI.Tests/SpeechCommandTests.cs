using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Business.Services.Base.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace AtaraxiaAI.Tests;

[TestClass]
public sealed class SpeechCommandTests
{
    private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(5);

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task PunctuatedJokeCommandReturnsFromRecognitionCallbackBeforeFetchingAndPlaying()
    {
        TaskCompletionSource<Joke> pendingJoke = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource fetching = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource played = new(TaskCreationOptions.RunContinuationsAsynchronously);
        FakeIntegrationFactory factory = new()
        {
            JokeService = Proxy<IJokeService>.Create((method, _) =>
            {
                fetching.TrySetResult();
                return pendingJoke.Task;
            })
        };
        IAudioPlayer player = Proxy<IAudioPlayer>.Create((method, _) =>
        {
            played.TrySetResult();
            return Task.CompletedTask;
        });
        SpeechEngine engine = new(factory, player, TestDependencies.Create());
        await engine.SelectSynthesizerAsync(SpeechEngine.DEFAULT_SYNTHESIZER, cancellationToken: TestContext.CancellationToken);
        engine.ActivateSpeechRecognition();

        // This must return even while the joke request has not completed.
        await Task.Run(() => factory.Recognizer.Fire("Hey Robot, tell me a joke."), TestContext.CancellationToken).WaitAsync(TIMEOUT, TestContext.CancellationToken);
        await fetching.Task.WaitAsync(TIMEOUT, TestContext.CancellationToken);
        Assert.AreEqual(0, factory.Recognizer.PauseCalls);
        pendingJoke.SetResult(new Joke { JokeType = Joke.JokeTypes.Single, JokeLine = "A joke" });
        await played.Task.WaitAsync(TIMEOUT, TestContext.CancellationToken);
        Assert.AreEqual(0, factory.Recognizer.PauseCalls);
    }
}
