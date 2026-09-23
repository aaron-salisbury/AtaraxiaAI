using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    // Providers return a complete WAV file. Playback and output selection belong to the host.
    public interface ISynthesizer
    {
        bool IsAvailable();
        Task<byte[]> SynthesizeAsync(string message, CancellationToken cancellationToken);
    }

    public interface IAudioPlayer
    {
        Task PlayAsync(byte[] wav, CancellationToken cancellationToken);
    }
}
