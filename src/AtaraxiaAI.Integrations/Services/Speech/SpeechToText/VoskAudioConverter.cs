using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.IO;

namespace AtaraxiaAI.Integrations.Services;

/// <summary>
/// Converts captured audio, such as a sound card's 48 kHz stereo float mix, into the 16 kHz, mono, 16-bit PCM that
/// Vosk recognizes. Audio already in that format passes through unchanged.
/// </summary>
internal sealed class VoskAudioConverter
{
    private readonly BufferedWaveProvider? _input;
    private readonly IWaveProvider? _output;
    private readonly byte[] _readBuffer = new byte[8192];

    internal VoskAudioConverter(WaveFormat inputFormat)
    {
        ArgumentNullException.ThrowIfNull(inputFormat);
        if (IsRecognizerFormat(inputFormat))
        {
            return;
        }

        _input = new BufferedWaveProvider(inputFormat) { DiscardOnBufferOverflow = true, ReadFully = false };
        ISampleProvider samples = _input.ToSampleProvider();
        if (samples.WaveFormat.Channels > 1)
        {
            samples = new MonoMixSampleProvider(samples);
        }

        if (samples.WaveFormat.SampleRate != VoskRecognizer.SAMPLE_RATE)
        {
            samples = new WdlResamplingSampleProvider(samples, VoskRecognizer.SAMPLE_RATE);
        }

        _output = samples.ToWaveProvider16();
    }

    internal byte[] Convert(byte[] buffer, int count)
    {
        if (_input is null || _output is null)
        {
            return buffer.AsSpan(0, count).ToArray();
        }

        _input.AddSamples(buffer, 0, count);
        using MemoryStream converted = new();
        int read;
        while ((read = _output.Read(_readBuffer, 0, _readBuffer.Length)) > 0)
        {
            converted.Write(_readBuffer, 0, read);
        }

        return converted.ToArray();
    }

    private static bool IsRecognizerFormat(WaveFormat format)
    {
        return format.Encoding == WaveFormatEncoding.Pcm
            && format.SampleRate == VoskRecognizer.SAMPLE_RATE
            && format.Channels == 1
            && format.BitsPerSample == 16;
    }

    /// <summary>Averages all channels into one.</summary>
    private sealed class MonoMixSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly int _channels;
        private float[] _sourceBuffer = [];

        public MonoMixSampleProvider(ISampleProvider source)
        {
            _source = source;
            _channels = source.WaveFormat.Channels;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 1);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int needed = count * _channels;
            if (_sourceBuffer.Length < needed)
            {
                _sourceBuffer = new float[needed];
            }

            int frames = _source.Read(_sourceBuffer, 0, needed) / _channels;
            for (int frame = 0; frame < frames; frame++)
            {
                float sum = 0;
                for (int channel = 0; channel < _channels; channel++)
                {
                    sum += _sourceBuffer[frame * _channels + channel];
                }

                buffer[offset + frame] = sum / _channels;
            }

            return frames;
        }
    }
}
