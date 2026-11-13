using AtaraxiaAI.Business.Componants;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Presentation.Desktop.Settings;

/// <summary>The user's speech recognition choices from the settings overlay.</summary>
/// <param name="Source">The chosen recognizer source.</param>
public sealed record RecognitionSettings(SpeechRecognizers Source = SpeechEngine.DEFAULT_RECOGNIZER);
