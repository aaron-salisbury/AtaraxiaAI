namespace AtaraxiaAI.Presentation.Desktop.Settings;

/// <summary>User preferences for this desktop app, kept between launches.</summary>
/// <remarks>Follows QmlativDevAssistant's desktop settings.</remarks>
public interface IDesktopSettings
{
    SpeechSettings GetSpeechSettings();

    void SetSpeechSettings(SpeechSettings settings);
}
