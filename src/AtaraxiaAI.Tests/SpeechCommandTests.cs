using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Business.Services.Base.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RunnethOverStudio.AppToolkit.Modules.Access;
using Serilog;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Tests;

[TestClass]
public sealed class SpeechCommandTests
{
    [TestMethod]
    public async Task PunctuatedJokeCommandReturnsFromRecognitionCallbackBeforeFetchingAndPlaying()
    {
        var pendingJoke = new TaskCompletionSource<Joke>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fetching = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var played = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recognizer = new TestRecognizer();
        var jokeService = Proxy<IJokeService>.Create((method, _) =>
        {
            fetching.TrySetResult();
            return pendingJoke.Task;
        });
        var synthesizer = Proxy<ISynthesizer>.Create((method, _) => method.Name switch
        {
            "IsAvailable" => true,
            "SynthesizeAsync" => Task.FromResult(new byte[] { 1, 2, 3 }),
            _ => throw new InvalidOperationException(method.Name)
        });
        var player = Proxy<IAudioPlayer>.Create((method, _) =>
        {
            played.TrySetResult();
            return Task.CompletedTask;
        });
        var factory = Proxy<IIntegrationFactory>.Create((method, _) => method.Name switch
        {
            "CreateAnswerProvider" => null,
            "CreateStreamingAvailabilityService" => null,
            "CreateRecognizer" => recognizer,
            "CreateSynthesizer" => synthesizer,
            "CreateJokeService" => jokeService,
            _ => throw new InvalidOperationException(method.Name)
        });
        var dependencies = new IntegrationDependencies(
            Proxy<IHttpRequester>.Create((method, _) => throw new InvalidOperationException(method.Name)),
            new LoggerConfiguration().CreateLogger());
        var engine = new SpeechEngine(factory, player, dependencies);
        engine.ActivateSpeechRecognition();

        // This must return even while the joke request has not completed.
        await Task.Run(() => recognizer.Fire("Hey Robot, tell me a joke.")).WaitAsync(TimeSpan.FromSeconds(5));
        await fetching.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(0, recognizer.PauseCalls);
        pendingJoke.SetResult(new Joke { JokeType = Joke.JokeTypes.Single, JokeLine = "A joke" });
        await played.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(0, recognizer.PauseCalls);
    }

    private sealed class TestRecognizer : IRecognizer
    {
        private Action<string>? _callback;
        public int PauseCalls { get; private set; }
        public bool IsAvailable() => true;
        public void Listen(Action<string> speechRecognizedAction) => _callback = speechRecognizedAction;
        public void Fire(string text) => _callback!(text);
        public void UpdateCaptureSource(AtaraxiaAI.Business.Base.Enums.SoundCaptureSources source) { }
        public void Pause() => PauseCalls++;
        public void Unpause() { }
        public void Dispose() { }
    }

    public class Proxy<T> : DispatchProxy where T : class
    {
        private Func<MethodInfo, object?[]?, object?> _invoke = null!;
        public static T Create(Func<MethodInfo, object?[]?, object?> invoke)
        {
            T instance = DispatchProxy.Create<T, Proxy<T>>();
            ((Proxy<T>)(object)instance)._invoke = invoke;
            return instance;
        }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            _invoke(targetMethod!, args);
    }
}
