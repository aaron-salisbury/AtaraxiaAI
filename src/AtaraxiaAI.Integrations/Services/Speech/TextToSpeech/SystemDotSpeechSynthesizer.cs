using AtaraxiaAI.Business.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations.Services;

internal class SystemDotSpeechSynthesizer : ISynthesizer
{
    private readonly CultureInfo _culture;

    internal SystemDotSpeechSynthesizer(CultureInfo culture, IntegrationDependencies context)
    {
        _culture = culture ?? new CultureInfo("en-US");
    }

    // The system's default voice is used; its installed voices are not offered as choices.
    IReadOnlyList<SpeechVoice> ISynthesizer.Voices => [];

    bool ISynthesizer.IsAvailable()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    Task<bool> ISynthesizer.PrepareAsync(IProgress<double>? progress, System.Threading.CancellationToken cancellationToken)
    {
        return Task.FromResult(((ISynthesizer)this).IsAvailable());
    }

    Task<byte[]> ISynthesizer.SynthesizeAsync(string message, string? voiceId, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using (SpeechSynthesizer synthesizer = new())
            using (MemoryStream audioStream = new())
            {
                synthesizer.SetOutputToWaveStream(audioStream);

                PromptBuilder promptBuilder = new();
                promptBuilder.StartVoice(_culture);
                promptBuilder.AppendText(message);
                promptBuilder.EndVoice();

                synthesizer.Speak(promptBuilder);
                return Task.FromResult(audioStream.ToArray());
            }
        }

        return Task.FromResult<byte[]>(null);
    }
}
