using System.Text.Json.Serialization;

namespace AtaraxiaAI.Integrations.DTOs;

/// <summary>The text of a Vosk recognition result.</summary>
internal sealed record VoskTranscript([property: JsonPropertyName("text")] string? Text);
