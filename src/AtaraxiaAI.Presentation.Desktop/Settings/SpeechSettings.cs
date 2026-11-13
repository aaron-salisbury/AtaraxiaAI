using AtaraxiaAI.Business.Componants;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Presentation.Desktop.Settings;

/// <summary>The user's speech choices from the settings overlay.</summary>
/// <param name="Source">The chosen speech source.</param>
/// <param name="VoiceGender">Which voices the voice list shows.</param>
/// <param name="VoiceId">The chosen voice, or <see langword="null"/> for the speech source's default voice.</param>
public sealed record SpeechSettings(
    SpeechSynthesizers Source = SpeechEngine.DEFAULT_SYNTHESIZER,
    VoiceGenderFilter VoiceGender = VoiceGenderFilter.Both,
    string? VoiceId = null);
