using AtaraxiaAI.Business.Services;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations.Services
{
    internal sealed class KokoroSynthesizer : ISynthesizer
    {
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
                string workerDirectory = Path.Combine(AppContext.BaseDirectory, "KokoroWorker");
                string workerExecutable = Path.Combine(workerDirectory,
                    OperatingSystem.IsWindows() ? "AtaraxiaAI.KokoroWorker.exe" : "AtaraxiaAI.KokoroWorker");
                ProcessStartInfo start;
                if (File.Exists(workerExecutable))
                    start = new ProcessStartInfo(workerExecutable);
                else
                {
                    string workerAssembly = Path.Combine(workerDirectory, "AtaraxiaAI.KokoroWorker.dll");
                    if (!File.Exists(workerAssembly))
                        throw new FileNotFoundException("Kokoro worker is missing from the desktop application output.", workerAssembly);
                    start = new ProcessStartInfo("dotnet");
                    start.ArgumentList.Add(workerAssembly);
                }
                start.UseShellExecute = false;
                start.CreateNoWindow = true;
                start.RedirectStandardError = true;
                start.ArgumentList.Add(request);
                start.ArgumentList.Add(response);
                _dependencies.Logger.Information("Starting isolated Kokoro model worker.");
                using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start Kokoro model worker.");
                process.ErrorDataReceived += (_, output) =>
                {
                    if (output.Data is { Length: > 0 } line)
                        _dependencies.Logger.Information("Kokoro worker: {Message}", line);
                };
                process.BeginErrorReadLine();
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

    }
}
