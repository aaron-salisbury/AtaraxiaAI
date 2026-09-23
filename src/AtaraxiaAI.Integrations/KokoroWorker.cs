using AtaraxiaAI.Integrations.Services;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations;

public static class KokoroWorker
{
    public static Task SynthesizeToFileAsync(string request, string response) =>
        KokoroSynthesizer.RunWorkerAsync(request, response);
}
