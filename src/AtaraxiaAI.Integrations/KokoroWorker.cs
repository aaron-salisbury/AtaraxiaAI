using AtaraxiaAI.Integrations.Services;
using Microsoft.ML.OnnxRuntime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations;

public static class KokoroWorker
{
    public static void VerifyNativeRuntime()
    {
        using var options = new SessionOptions();
    }

    public static string DescribeNativeLibraries()
    {
        string[] names = ["onnxruntime.dll", "msvcp140.dll", "vcruntime140.dll", "vcruntime140_1.dll"];
        var lines = new List<string>();
        foreach (string directory in new[] { AppContext.BaseDirectory, Environment.SystemDirectory })
        {
            foreach (string name in names)
            {
                string path = Path.Combine(directory, name);
                if (File.Exists(path))
                    lines.Add($"File: {path} (version {FileVersionInfo.GetVersionInfo(path).FileVersion})");
            }
        }
        using var process = Process.GetCurrentProcess();
        foreach (ProcessModule module in process.Modules.Cast<ProcessModule>()
                     .Where(module => names.Contains(module.ModuleName, StringComparer.OrdinalIgnoreCase)))
            lines.Add($"Loaded: {module.FileName} (version {module.FileVersionInfo.FileVersion})");
        return string.Join(Environment.NewLine, lines);
    }

    public static Task SynthesizeToFileAsync(string request, string response) =>
        KokoroSynthesizer.RunWorkerAsync(request, response);
}
