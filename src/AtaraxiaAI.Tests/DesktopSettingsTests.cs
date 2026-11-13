using AtaraxiaAI.Presentation.Desktop.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Tests;

[TestClass]
public sealed class DesktopSettingsTests
{
    private string _directory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "AtaraxiaAI.Tests", Guid.NewGuid().ToString("N"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [TestMethod]
    public void WithNothingStoredSystemSpeechWithAllVoicesIsTheDefault()
    {
        SpeechSettings speech = new DesktopSettings(_directory).GetSpeechSettings();

        Assert.AreEqual(new SpeechSettings(SpeechSynthesizers.SystemDotSpeech, VoiceGenderFilter.Both, null), speech);
    }

    [TestMethod]
    public void SpeechChoicesRoundTripByName()
    {
        SpeechSettings chosen = new(SpeechSynthesizers.Kokoro, VoiceGenderFilter.Male, "am_adam");

        new DesktopSettings(_directory).SetSpeechSettings(chosen);
        string json = File.ReadAllText(Path.Combine(_directory, "desktop-settings.json"));

        Assert.AreEqual(chosen, new DesktopSettings(_directory).GetSpeechSettings());
        // Enums are stored by name, so reordering them cannot change a stored choice.
        Assert.Contains("\"Kokoro\"", json);
        Assert.Contains("\"Male\"", json);
    }

    [TestMethod]
    public void AnUnreadableOrOutdatedFileFallsBackToTheDefaults()
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "desktop-settings.json");

        File.WriteAllText(path, "{ not json");
        Assert.AreEqual(new SpeechSettings(), new DesktopSettings(_directory).GetSpeechSettings());

        // A source that has since been removed, such as Bing.
        File.WriteAllText(path, "{ \"Speech\": { \"Source\": \"MicrosoftBing\" } }");
        Assert.AreEqual(new SpeechSettings(), new DesktopSettings(_directory).GetSpeechSettings());
    }

    [TestMethod]
    public void WithNothingStoredSystemRecognitionIsTheDefault()
    {
        Assert.AreEqual(new RecognitionSettings(SpeechRecognizers.SystemDotSpeech), new DesktopSettings(_directory).GetRecognitionSettings());
    }

    [TestMethod]
    public void RecognitionAndSpeechChoicesAreKeptSeparately()
    {
        DesktopSettings settings = new(_directory);
        SpeechSettings speech = new(SpeechSynthesizers.Kokoro, VoiceGenderFilter.Female, "bf_emma");

        settings.SetRecognitionSettings(new RecognitionSettings(SpeechRecognizers.Vosk));
        settings.SetSpeechSettings(speech);

        DesktopSettings reopened = new(_directory);
        Assert.AreEqual(new RecognitionSettings(SpeechRecognizers.Vosk), reopened.GetRecognitionSettings());
        Assert.AreEqual(speech, reopened.GetSpeechSettings());
    }

    [TestMethod]
    public void AnOutdatedSectionDoesNotLoseTheOthers()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            Path.Combine(_directory, "desktop-settings.json"),
            "{ \"Recognition\": { \"Source\": \"Vosk\" }, \"Speech\": { \"Source\": \"MicrosoftBing\" } }");
        DesktopSettings settings = new(_directory);

        Assert.AreEqual(new SpeechSettings(), settings.GetSpeechSettings());
        settings.SetSpeechSettings(new SpeechSettings(SpeechSynthesizers.Kokoro));

        Assert.AreEqual(new RecognitionSettings(SpeechRecognizers.Vosk), new DesktopSettings(_directory).GetRecognitionSettings());
    }
}
