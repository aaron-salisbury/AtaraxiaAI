using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AtaraxiaAI.Presentation.Desktop.Settings;

/// <summary>Keeps desktop preferences in desktop-settings.json in the application data directory.</summary>
/// <remarks>Follows QmlativDevAssistant's DesktopSettings.</remarks>
internal sealed class DesktopSettings : IDesktopSettings
{
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

    public SpeechSettings GetSpeechSettings()
    {
        return Read().Speech ?? new SpeechSettings();
    }

    public void SetSpeechSettings(SpeechSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        DesktopSettingsData data = Read();
        data.Speech = settings;
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(data, SERIALIZE_OPTIONS));
    }

    private DesktopSettingsData Read()
    {
        if (!File.Exists(_settingsPath))
        {
            return new DesktopSettingsData();
        }

        try
        {
            string json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<DesktopSettingsData>(json, SERIALIZE_OPTIONS)
                ?? new DesktopSettingsData();
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException)
        {
            // A corrupt or inaccessible file, or one naming a speech source that no longer exists, falls back to the
            // defaults rather than preventing the app from starting.
            return new DesktopSettingsData();
        }
    }

    private sealed class DesktopSettingsData
    {
        public SpeechSettings? Speech { get; set; }
    }
}
