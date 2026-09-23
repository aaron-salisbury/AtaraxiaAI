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
        private static readonly object ModelGate = new();
        private static Task<KokoroWavSynthesizer>? _modelTask;

        private static Task<KokoroWavSynthesizer> GetModelAsync(IntegrationDependencies dependencies)
        {
            lock (ModelGate)
                return _modelTask ??= Task.Run(async () =>
                {
                    dependencies.Logger.Information("Loading Kokoro speech model (downloading if needed).");
                    int lastReportedTenPercent = 0;
                    try
                    {
                        var model = await KokoroWavSynthesizer.LoadModelAsync(
                            OnDownloadProgress: progress =>
                            {
                                int tenPercent = (int)(progress * 10);
                                if (tenPercent > lastReportedTenPercent)
                                {
                                    lastReportedTenPercent = tenPercent;
                                    dependencies.Logger.Information("Kokoro model download: {Percent}%.", tenPercent * 10);
                                }
                            });
                        dependencies.Logger.Information("Kokoro speech model ready.");
                        return model;
                    }
                    catch (Exception ex)
                    {
                        dependencies.Logger.Error(ex, "Kokoro model preparation failed; speech will use an available fallback.");
                        throw;
                    }
                });
        }

        internal static async Task PrepareAsync(IntegrationDependencies dependencies)
        {
            try { await GetModelAsync(dependencies); }
            catch { /* The speech engine observes the same failure and chooses a fallback. */ }
        }
        private readonly CultureInfo _culture;
        private readonly IntegrationDependencies _dependencies;

        internal KokoroSynthesizer(CultureInfo culture, IntegrationDependencies context)
        {
            _culture = culture ?? new CultureInfo("en-US");
            _dependencies = context;
        }

        public bool IsAvailable() => _culture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase);

        public async Task<byte[]> SynthesizeAsync(string message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(message)) return Array.Empty<byte>();
            var model = await GetModelAsync(_dependencies).WaitAsync(cancellationToken);
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
