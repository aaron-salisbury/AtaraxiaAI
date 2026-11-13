using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Business.Services;

/// <summary>A voice a speech provider can speak with.</summary>
public sealed record SpeechVoice(string Id, string DisplayName, VoiceGender Gender)
{
    public override string ToString()
    {
        return DisplayName;
    }
}

// Providers return a complete WAV file. Playback and output selection belong to the host.
public interface ISynthesizer
{
    /// <summary>
    /// The voices to choose from, the default first. Empty when the provider offers no choice of voice.
    /// </summary>
    IReadOnlyList<SpeechVoice> Voices { get; }

    bool IsAvailable();

    /// <summary>
    /// Readies the provider before first use, such as downloading a local model.
    /// </summary>
    /// <param name="progress">Receives download progress from 0 to 100 when the provider must download something.</param>
    /// <returns>Whether the provider is ready; reasons it is not are logged.</returns>
    Task<bool> PrepareAsync(IProgress<double>? progress, CancellationToken cancellationToken);

    /// <param name="voiceId">One of <see cref="Voices"/>; the default voice is used when it is not one of them.</param>
    Task<byte[]> SynthesizeAsync(string message, string? voiceId, CancellationToken cancellationToken);
}

public interface IAudioPlayer
{
    Task PlayAsync(byte[] wav, CancellationToken cancellationToken);
}
