using AtaraxiaAI.Business.Services;
using NAudio.Wave;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.DesktopApp;

// Owns the output device for the duration of each utterance.
internal sealed class WavAudioPlayer : IAudioPlayer
{
    public async Task PlayAsync(byte[] wav, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(wav, writable: false);
        using var reader = new WaveFileReader(stream);
        using var pcm = WaveFormatConversionStream.CreatePcmStream(reader);
        using var aligned = new BlockAlignReductionStream(pcm);
        using var output = new WaveOutEvent();
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        output.PlaybackStopped += (_, args) =>
        {
            if (args.Exception != null) completion.TrySetException(args.Exception);
            else completion.TrySetResult(true);
        };
        output.Init(aligned);
        cancellationToken.ThrowIfCancellationRequested();
        output.Play();
        using var registration = cancellationToken.Register(output.Stop);
        await completion.Task.WaitAsync(cancellationToken);
    }
}
