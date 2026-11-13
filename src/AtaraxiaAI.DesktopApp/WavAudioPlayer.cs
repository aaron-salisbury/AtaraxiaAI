using AtaraxiaAI.Business.Services;
using NAudio.Wave;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.DesktopApp;

// Owns the output device for the duration of each utterance.
internal sealed class WavAudioPlayer : IAudioPlayer
{
    private readonly SemaphoreSlim _playbackLock = new(1, 1);
    private readonly int _deviceNumber;

    // -1 selects the system default output device. The host owns future device selection.
    public WavAudioPlayer(int deviceNumber = -1)
    {
        _deviceNumber = deviceNumber;
    }

    public async Task PlayAsync(byte[] wav, CancellationToken cancellationToken)
    {
        await _playbackLock.WaitAsync(cancellationToken);
        try
        {
            using MemoryStream stream = new(wav, writable: false);
            using WaveFileReader reader = new(stream);
            using WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(reader);
            using BlockAlignReductionStream aligned = new(pcm);
            using WaveOutEvent output = new() { DeviceNumber = _deviceNumber };
            TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            output.PlaybackStopped += (_, args) =>
            {
                if (args.Exception != null)
                {
                    completion.TrySetException(args.Exception);
                }
                else
                {
                    completion.TrySetResult(true);
                }
            };
            output.Init(aligned);
            cancellationToken.ThrowIfCancellationRequested();
            output.Play();
            using CancellationTokenRegistration registration = cancellationToken.Register(output.Stop);
            await completion.Task.WaitAsync(cancellationToken);
        }
        finally
        {
            _playbackLock.Release();
        }
    }
}
