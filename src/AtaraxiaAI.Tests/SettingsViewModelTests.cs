using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using AtaraxiaAI.Presentation.Desktop.Base;
using AtaraxiaAI.Presentation.Desktop.Settings;
using AtaraxiaAI.Presentation.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Tests;

[TestClass]
public sealed class SettingsViewModelTests
{
    private static readonly SpeechVoice HEART = new("af_heart", "Heart (American)", VoiceGender.Female);
    private static readonly SpeechVoice ADAM = new("am_adam", "Adam (American)", VoiceGender.Male);
    private static readonly SpeechVoice EMMA = new("bf_emma", "Emma (British)", VoiceGender.Female);

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task SpeechSourceOffersSystemAndKokoroWithSystemSelectedAndNoVoiceChoices()
    {
        (SettingsViewModel settings, _, _) = await CreateAsync();

        CollectionAssert.AreEqual(new[] { "System", "Vosk" }, settings.RecognizerSourceTypes.Select(item => item.Text).ToArray());
        CollectionAssert.AreEqual(new[] { "System", "Kokoro" }, settings.SpeechSourceTypes.Select(item => item.Text).ToArray());
        Assert.AreEqual("System", settings.SelectedRecognizerSource.Text);
        CollectionAssert.AreEqual(new[] { "Male", "Female", "Both" }, settings.VoiceGenderTypes.Select(item => item.Text).ToArray());
        Assert.AreEqual("System", settings.SelectedSpeechSource.Text);
        Assert.AreEqual("Both", settings.SelectedVoiceGender.Text);
        Assert.IsFalse(settings.HasVoiceChoices);
    }

    [TestMethod]
    public async Task ChoosingKokoroShowsDownloadProgressThenItsVoicesAndStoresTheChoice()
    {
        TaskCompletionSource finishDownload = new(TaskCreationOptions.RunContinuationsAsynchronously);
        (SettingsViewModel settings, AI ai, MemoryDesktopSettings stored) = await CreateAsync(factory =>
            factory.Synthesizers[SpeechSynthesizers.Kokoro].Prepare = async (progress, token) =>
            {
                progress?.Report(42);
                await finishDownload.Task.WaitAsync(token);
                return true;
            });

        settings.SelectedSpeechSource = SourceItem(settings, SpeechSynthesizers.Kokoro);
        await TestDependencies.WaitUntilAsync(() => settings.SpeechPreparation.IsProgressKnown);

        Assert.IsTrue(settings.SpeechPreparation.IsPreparing);
        Assert.IsFalse(settings.HasVoiceChoices);
        Assert.AreEqual(42d, settings.SpeechPreparation.Progress);
        Assert.AreEqual("Downloading Kokoro… 42%", settings.SpeechPreparation.Status);
        finishDownload.SetResult();
        await TestDependencies.WaitUntilAsync(() => !settings.SpeechPreparation.IsPreparing);
        Assert.IsNull(settings.SpeechPreparation.Status);
        Assert.AreEqual(SpeechSynthesizers.Kokoro, ai.SpeechEngine.ActiveSynthesizer);
        Assert.AreEqual(SpeechSynthesizers.Kokoro, stored.Speech.Source);
        Assert.IsTrue(settings.HasVoiceChoices);
        CollectionAssert.AreEqual(new[] { HEART, ADAM, EMMA }, settings.Voices);
        // With no stored voice, the default voice is shown as chosen.
        Assert.AreEqual(HEART, settings.SelectedVoice);
    }

    [TestMethod]
    public async Task ChoosingAnotherSourceDuringADownloadCancelsIt()
    {
        CancellationToken downloadToken = default;
        (SettingsViewModel settings, AI ai, _) = await CreateAsync(factory =>
            factory.Synthesizers[SpeechSynthesizers.Kokoro].Prepare = async (progress, token) =>
            {
                downloadToken = token;
                progress?.Report(10);
                await Task.Delay(Timeout.Infinite, token);
                return true;
            });

        settings.SelectedSpeechSource = SourceItem(settings, SpeechSynthesizers.Kokoro);
        await TestDependencies.WaitUntilAsync(() => settings.SpeechPreparation.IsProgressKnown);
        settings.SelectedSpeechSource = SourceItem(settings, SpeechSynthesizers.SystemDotSpeech);
        await TestDependencies.WaitUntilAsync(() => !settings.SpeechPreparation.IsPreparing);

        Assert.IsTrue(downloadToken.IsCancellationRequested);
        Assert.AreEqual(SpeechSynthesizers.SystemDotSpeech, ai.SpeechEngine.ActiveSynthesizer);
        Assert.AreEqual("System", settings.SelectedSpeechSource.Text);
    }

    [TestMethod]
    public async Task AnUnavailableSourceIsReportedNotStoredAndTheSelectionReturnsToTheSourceInUse()
    {
        (SettingsViewModel settings, _, MemoryDesktopSettings stored) = await CreateAsync(factory =>
            factory.Synthesizers[SpeechSynthesizers.Kokoro].Prepare = (_, _) => Task.FromResult(false));

        settings.SelectedSpeechSource = SourceItem(settings, SpeechSynthesizers.Kokoro);
        await TestDependencies.WaitUntilAsync(() => !settings.SpeechPreparation.IsPreparing);

        Assert.AreEqual("System", settings.SelectedSpeechSource.Text);
        Assert.AreEqual("Kokoro is unavailable. See the logs for details.", settings.SpeechPreparation.Status);
        Assert.AreEqual(SpeechSynthesizers.SystemDotSpeech, stored.Speech.Source);
    }

    [TestMethod]
    public async Task StoredChoicesAreAppliedAtStartupWithoutPlayingASample()
    {
        MemoryDesktopSettings stored = new()
        {
            Recognition = new RecognitionSettings(SpeechRecognizers.Vosk),
            Speech = new SpeechSettings(SpeechSynthesizers.Kokoro, VoiceGenderFilter.Male, ADAM.Id)
        };
        (SettingsViewModel settings, AI ai, FakeIntegrationFactory factory) = await CreateWithFactoryAsync(stored);

        await settings.ApplyStoredSettingsAsync();

        Assert.AreEqual("Vosk", settings.SelectedRecognizerSource.Text);
        Assert.AreEqual(SpeechRecognizers.Vosk, ai.SpeechEngine.ActiveRecognizer);

        Assert.AreEqual("Kokoro", settings.SelectedSpeechSource.Text);
        Assert.AreEqual("Male", settings.SelectedVoiceGender.Text);
        Assert.AreEqual(SpeechSynthesizers.Kokoro, ai.SpeechEngine.ActiveSynthesizer);
        Assert.AreEqual(ADAM.Id, ai.SpeechEngine.VoiceId);
        Assert.AreEqual(ADAM, settings.SelectedVoice);
        Assert.IsEmpty(factory.Synthesizers[SpeechSynthesizers.Kokoro].Spoken);
    }

    [TestMethod]
    public async Task TheGenderFilterNarrowsTheVoicesAndIsStored()
    {
        (SettingsViewModel settings, _, MemoryDesktopSettings stored) = await CreateAsync();
        settings.SelectedSpeechSource = SourceItem(settings, SpeechSynthesizers.Kokoro);
        await TestDependencies.WaitUntilAsync(() => settings.HasVoiceChoices);

        settings.SelectedVoiceGender = GenderItem(settings, VoiceGenderFilter.Male);
        CollectionAssert.AreEqual(new[] { ADAM }, settings.Voices);
        settings.SelectedVoiceGender = GenderItem(settings, VoiceGenderFilter.Female);
        CollectionAssert.AreEqual(new[] { HEART, EMMA }, settings.Voices);

        Assert.AreEqual(VoiceGenderFilter.Female, stored.Speech.VoiceGender);
    }

    [TestMethod]
    public async Task ChoosingAVoiceStoresItUsesItAndPlaysASample()
    {
        MemoryDesktopSettings stored = new();
        (SettingsViewModel settings, AI ai, FakeIntegrationFactory factory) = await CreateWithFactoryAsync(stored);
        settings.SelectedSpeechSource = SourceItem(settings, SpeechSynthesizers.Kokoro);
        await TestDependencies.WaitUntilAsync(() => settings.HasVoiceChoices);

        settings.SelectedVoice = EMMA;
        await TestDependencies.WaitUntilAsync(() => factory.Synthesizers[SpeechSynthesizers.Kokoro].SpokenVoices.Count == 1);

        Assert.AreEqual(EMMA.Id, stored.Speech.VoiceId);
        Assert.AreEqual(EMMA.Id, ai.SpeechEngine.VoiceId);
        Assert.AreEqual(EMMA.Id, factory.Synthesizers[SpeechSynthesizers.Kokoro].SpokenVoices[0]);
    }

    [TestMethod]
    public async Task ChoosingVoskShowsDownloadProgressThenInstallingAndStoresTheChoice()
    {
        TaskCompletionSource finishDownload = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource downloaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        (SettingsViewModel settings, AI ai, MemoryDesktopSettings stored) = await CreateAsync(factory =>
            factory.Recognizers[SpeechRecognizers.Vosk].Prepare = async (progress, token) =>
            {
                progress?.Report(30);
                await finishDownload.Task.WaitAsync(token);
                progress?.Report(100);
                await downloaded.Task.WaitAsync(token);
                return true;
            });

        settings.SelectedRecognizerSource = RecognizerItem(settings, SpeechRecognizers.Vosk);
        await TestDependencies.WaitUntilAsync(() => settings.RecognizerPreparation.IsProgressKnown);

        Assert.IsTrue(settings.RecognizerPreparation.IsPreparing);
        Assert.AreEqual("Downloading Vosk… 30%", settings.RecognizerPreparation.Status);
        // Recognizer and speech sources are prepared independently.
        Assert.IsFalse(settings.SpeechPreparation.IsPreparing);
        finishDownload.SetResult();
        await TestDependencies.WaitUntilAsync(() => settings.RecognizerPreparation.Status == "Installing Vosk…");
        downloaded.SetResult();
        await TestDependencies.WaitUntilAsync(() => !settings.RecognizerPreparation.IsPreparing);

        Assert.IsNull(settings.RecognizerPreparation.Status);
        Assert.AreEqual(SpeechRecognizers.Vosk, ai.SpeechEngine.ActiveRecognizer);
        Assert.AreEqual(SpeechRecognizers.Vosk, stored.Recognition.Source);
    }

    [TestMethod]
    public async Task AnUnavailableRecognizerIsReportedNotStoredAndTheSelectionReturnsToSystem()
    {
        (SettingsViewModel settings, AI ai, MemoryDesktopSettings stored) = await CreateAsync(factory =>
            factory.Recognizers[SpeechRecognizers.Vosk].Prepare = (_, _) => Task.FromResult(false));

        settings.SelectedRecognizerSource = RecognizerItem(settings, SpeechRecognizers.Vosk);
        await TestDependencies.WaitUntilAsync(() => !settings.RecognizerPreparation.IsPreparing);

        Assert.AreEqual("System", settings.SelectedRecognizerSource.Text);
        Assert.AreEqual("Vosk is unavailable. See the logs for details.", settings.RecognizerPreparation.Status);
        Assert.AreEqual(SpeechRecognizers.SystemDotSpeech, ai.SpeechEngine.ActiveRecognizer);
        Assert.AreEqual(SpeechRecognizers.SystemDotSpeech, stored.Recognition.Source);
    }

    private static ComboBoxEnumItem RecognizerItem(SettingsViewModel settings, SpeechRecognizers kind)
    {
        return settings.RecognizerSourceTypes.Single(item => item.Value == (int)kind);
    }

    private static ComboBoxEnumItem SourceItem(SettingsViewModel settings, SpeechSynthesizers kind)
    {
        return settings.SpeechSourceTypes.Single(item => item.Value == (int)kind);
    }

    private static ComboBoxEnumItem GenderItem(SettingsViewModel settings, VoiceGenderFilter filter)
    {
        return settings.VoiceGenderTypes.Single(item => item.Value == (int)filter);
    }

    private static async Task<(SettingsViewModel Settings, AI AI, MemoryDesktopSettings Stored)> CreateAsync(System.Action<FakeIntegrationFactory>? configure = null)
    {
        MemoryDesktopSettings stored = new();
        (SettingsViewModel settings, AI ai, _) = await CreateWithFactoryAsync(stored, configure);
        return (settings, ai, stored);
    }

    /// <summary>Creates the view model over an AI whose speech starts on the default source, as initialization leaves it.</summary>
    private static async Task<(SettingsViewModel Settings, AI AI, FakeIntegrationFactory Factory)> CreateWithFactoryAsync(MemoryDesktopSettings stored, System.Action<FakeIntegrationFactory>? configure = null)
    {
        FakeIntegrationFactory factory = new();
        factory.Synthesizers[SpeechSynthesizers.Kokoro].Voices = [HEART, ADAM, EMMA];
        configure?.Invoke(factory);
        IntegrationDependencies dependencies = TestDependencies.Create();
        AI ai = new(TestDependencies.Logger, factory, TestDependencies.SilentPlayer, dependencies)
        {
            SpeechEngine = new SpeechEngine(factory, TestDependencies.SilentPlayer, dependencies)
        };
        await ai.SpeechEngine.SelectSynthesizerAsync(SpeechEngine.DEFAULT_SYNTHESIZER);
        return (new SettingsViewModel(ai, stored), ai, factory);
    }
}
