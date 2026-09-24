using KokoroSharp;
using Microsoft.ML.OnnxRuntime;
using System;
using System.IO;
using System.Threading.Tasks;

if (args.Length == 2 && args[0] == "--probe")
{
    await File.WriteAllTextAsync(args[1], "entered probe");
    try
    {
        using var options = new SessionOptions();
        await File.WriteAllTextAsync(args[1], "ready");
    }
    catch (Exception error)
    {
        await File.WriteAllTextAsync(args[1], error.ToString());
        Environment.ExitCode = 1;
    }
    return;
}

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: AtaraxiaAI.KokoroWorker <request.txt> <response.wav>");
    Environment.ExitCode = 2;
    return;
}

string response = args[1];
try
{
    string message = await File.ReadAllTextAsync(args[0]);
    await File.WriteAllTextAsync(response + ".stage", "loading ONNX runtime");
    using (var options = new SessionOptions()) { }

    await File.WriteAllTextAsync(response + ".stage", "loading model");
    Console.Error.WriteLine("Loading Kokoro speech model (downloading if needed).");
    int lastReportedTenPercent = 0;
    var model = await KokoroWavSynthesizer.LoadModelAsync(OnDownloadProgress: progress =>
    {
        int tenPercent = (int)(progress * 10);
        if (tenPercent > lastReportedTenPercent)
        {
            lastReportedTenPercent = tenPercent;
            Console.Error.WriteLine($"Kokoro model download: {tenPercent * 10}%.");
        }
    });
    Console.Error.WriteLine("Kokoro speech model ready.");

    await File.WriteAllTextAsync(response + ".stage", "synthesizing audio");
    byte[] pcm = await model.SynthesizeAsync(message, KokoroVoiceManager.GetVoice("af_heart"));
    await File.WriteAllTextAsync(response + ".stage", "writing WAV");
    using var output = new MemoryStream();
    using (var writer = new BinaryWriter(output, System.Text.Encoding.ASCII, leaveOpen: true))
    {
        // KokoroSharp produces 24 kHz, mono, 16-bit PCM.
        writer.Write("RIFF"u8);
        writer.Write(36 + pcm.Length);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(24000);
        writer.Write(48000);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(pcm.Length);
        writer.Write(pcm);
    }
    await File.WriteAllBytesAsync(response, output.ToArray());
}
catch (Exception error)
{
    try { await File.WriteAllTextAsync(response + ".error", error.ToString()); }
    catch (IOException) { }
    Console.Error.WriteLine(error);
    Environment.ExitCode = 1;
}
