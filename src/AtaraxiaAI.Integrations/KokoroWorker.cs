using AtaraxiaAI.Integrations.Services;
using Microsoft.ML.OnnxRuntime;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations;

public static class KokoroWorker
{
    public static void VerifyNativeRuntime()
    {
        using var options = new SessionOptions();
    }

    public static Task SynthesizeToFileAsync(string request, string response) =>
        KokoroSynthesizer.RunWorkerAsync(request, response);
}
