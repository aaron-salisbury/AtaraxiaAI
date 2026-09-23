using AtaraxiaAI.Business.Services;
using KokoroSharp;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace AtaraxiaAI.Integrations.Services
{
    internal sealed class KokoroSynthesizer : ISynthesizer
    {
        private static readonly object ModelGate = new();
        private static Task<KokoroWavSynthesizer>? _modelTask;

        private static Task<KokoroWavSynthesizer> GetModelAsync(ILogger logger)
        {
            lock (ModelGate)
                return _modelTask ??= Task.Run(async () =>
                {
                    logger.Information("Loading Kokoro speech model (downloading if needed).");
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
                                    logger.Information("Kokoro model download: {Percent}%.", tenPercent * 10);
                                }
                            });
                        logger.Information("Kokoro speech model ready.");
                        return model;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Kokoro model preparation failed; speech will use an available fallback.");
                        throw;
                    }
                });
        }

        internal static async Task PrepareAsync(IntegrationDependencies dependencies)
        {
            try { await GetModelAsync(dependencies.Logger); }
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
            string directory = Path.Combine(Path.GetTempPath(), "AtaraxiaAI", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string request = Path.Combine(directory, "request.txt");
            string response = Path.Combine(directory, "response.wav");
            try
            {
                await File.WriteAllTextAsync(request, message, cancellationToken);
                string executable = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate the speech worker executable.");
                var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true };
                if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                    start.ArgumentList.Add(System.Reflection.Assembly.GetEntryAssembly()?.Location
                        ?? throw new InvalidOperationException("Cannot locate the application assembly."));
                start.ArgumentList.Add("--kokoro-worker");
                start.ArgumentList.Add(request);
                start.ArgumentList.Add(response);
                _dependencies.Logger.Information("Starting isolated Kokoro model worker.");
                using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start Kokoro model worker.");
                try { await process.WaitForExitAsync(cancellationToken); }
                catch (OperationCanceledException)
                {
                    if (!process.HasExited) process.Kill(entireProcessTree: true);
                    throw;
                }
                if (process.ExitCode != 0)
                {
                    string stage = File.Exists(response + ".stage")
                        ? await File.ReadAllTextAsync(response + ".stage", cancellationToken) : "before model loading";
                    string detail = File.Exists(response + ".error")
                        ? await File.ReadAllTextAsync(response + ".error", cancellationToken) : "No managed exception was recorded.";
                    throw new InvalidOperationException(
                        $"Kokoro worker exited with code 0x{process.ExitCode:X8} at '{stage}'. {detail}");
                }
                return await File.ReadAllBytesAsync(response, cancellationToken);
            }
            finally
            {
                try { Directory.Delete(directory, recursive: true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        internal static async Task RunWorkerAsync(string request, string response)
        {
            string message = await File.ReadAllTextAsync(request);
            await File.WriteAllTextAsync(response + ".stage", "loading model");
            var model = await GetModelAsync(Log.Logger);
            await File.WriteAllTextAsync(response + ".stage", "synthesizing audio");
            // KokoroSharp produces 24 kHz, mono PCM; wrap it in WAV for the shared player.
            byte[] pcm = await model.SynthesizeAsync(message, KokoroVoiceManager.GetVoice("af_heart"))
                .ConfigureAwait(false);
            await File.WriteAllTextAsync(response + ".stage", "writing WAV");
            using var output = new MemoryStream();
            using (var writer = new WaveFileWriter(output, new WaveFormat(24000, 16, 1)))
            {
                writer.Write(pcm, 0, pcm.Length);
            }
            await File.WriteAllBytesAsync(response, output.ToArray());
        }
    }
}
