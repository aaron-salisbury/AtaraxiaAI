using AtaraxiaAI.Business.Services;
using KokoroSharp;
using NAudio.Wave;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations.Services
{
    internal sealed class KokoroSynthesizer : ISynthesizer
    {
        private static readonly Lazy<Task<KokoroWavSynthesizer>> Model = new(
            () => KokoroWavSynthesizer.LoadModelAsync());
        private readonly CultureInfo _culture;

        internal KokoroSynthesizer(CultureInfo culture, SpeechProviderDependencies context) => _culture = culture ?? new CultureInfo("en-US");

        public bool IsAvailable() => _culture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase);

        public async Task<byte[]> SynthesizeAsync(string message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var model = await Model.Value.WaitAsync(cancellationToken);
            // KokoroSharp produces 24 kHz, mono PCM; wrap it in WAV for the shared player.
            byte[] pcm = await model.SynthesizeAsync(message, KokoroVoiceManager.GetVoice("af_heart"))
                .WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            using var output = new MemoryStream();
            using (var writer = new WaveFileWriter(output, new WaveFormat(24000, 16, 1)))
            {
                writer.Write(pcm, 0, pcm.Length);
            }
            return output.ToArray();
        }
    }
}
