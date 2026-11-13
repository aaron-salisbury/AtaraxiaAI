using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AtaraxiaAI.Presentation.Desktop.Settings;

/// <summary>Keeps desktop preferences in desktop-settings.json in the application data directory.</summary>
/// <remarks>
/// Follows QmlativDevAssistant's DesktopSettings. Each section is read and written on its own, so a section that can no
/// longer be read, such as one naming a removed speech source, falls back to its defaults without losing the others.
/// </remarks>
internal sealed class DesktopSettings : IDesktopSettings
{
    private const string RECOGNITION_SECTION = "Recognition";
    private const string SPEECH_SECTION = "Speech";

    private static readonly JsonSerializerOptions SERIALIZE_OPTIONS = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public DesktopSettings(string applicationDataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDataDirectory);
        _settingsPath = Path.Combine(applicationDataDirectory, "desktop-settings.json");
    }

    public RecognitionSettings GetRecognitionSettings()
    {
        return ReadSection<RecognitionSettings>(RECOGNITION_SECTION) ?? new RecognitionSettings();
    }

    public void SetRecognitionSettings(RecognitionSettings settings)
    {
        WriteSection(RECOGNITION_SECTION, settings);
    }

    public SpeechSettings GetSpeechSettings()
    {
        return ReadSection<SpeechSettings>(SPEECH_SECTION) ?? new SpeechSettings();
    }

    public void SetSpeechSettings(SpeechSettings settings)
    {
        WriteSection(SPEECH_SECTION, settings);
    }

    private T? ReadSection<T>(string section) where T : class
    {
        try
        {
            return ReadRoot()?[section]?.Deserialize<T>(SERIALIZE_OPTIONS);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void WriteSection<T>(string section, T settings) where T : class
    {
        ArgumentNullException.ThrowIfNull(settings);

        JsonObject root = ReadRoot() ?? [];
        root[section] = JsonSerializer.SerializeToNode(settings, SERIALIZE_OPTIONS);
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        File.WriteAllText(_settingsPath, root.ToJsonString(SERIALIZE_OPTIONS));
    }

    private JsonObject? ReadRoot()
    {
        if (!File.Exists(_settingsPath))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(File.ReadAllText(_settingsPath)) as JsonObject;
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException)
        {
            // A corrupt or inaccessible file falls back to the defaults rather than preventing the app from starting.
            return null;
        }
    }
}
