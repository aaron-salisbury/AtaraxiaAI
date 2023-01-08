using AtaraxiaAI.Business.Services.Base.Domains;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Timers;
using Vosk;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Business.Services
{
    internal class VoskRecognizer : IRecognizer
    {
        private const float SAMPLE_RATE = 16000f;

        internal SoundCaptureSources CaptureSource { get; set; }

        private WaveInEvent _micSource;
        private WasapiCapture _soundCardSource;
        private int _bytesPerSample;
        private List<double> _audio;
        private Timer _timer;
        private Action<string> _speechRecognizedAction;
        private Vosk.VoskRecognizer _recognizer;

        internal VoskRecognizer(SoundCaptureSources captureSource = SoundCaptureSources.SoundCard)
        {
            Vosk.Vosk.SetLogLevel(-1);

            _recognizer = new Vosk.VoskRecognizer(new Model(Data.CRUD.ReadVoskModelContentPath()), SAMPLE_RATE);
            _recognizer.SetMaxAlternatives(0);
            _recognizer.SetWords(true);

            CaptureSource = captureSource;
        }

        bool IRecognizer.IsAvailable() => true;

        void IRecognizer.Listen(Action<string> speechRecognizedAction)
        {
            _speechRecognizedAction = speechRecognizedAction;

            Dispose();
            BuildDisposables();

            if (CaptureSource == SoundCaptureSources.Microphone)
            {
                _micSource.DataAvailable += OnNewAudioData;
                _micSource.StartRecording();
            }
            else if (CaptureSource == SoundCaptureSources.SoundCard)
            {
                _soundCardSource.DataAvailable += OnNewAudioData;
                _soundCardSource.StartRecording();
            }

            _timer.Start();
        }

        void IRecognizer.Pause()
        {
            if (CaptureSource == SoundCaptureSources.Microphone)
            {
                _micSource.StopRecording();
            }
            else if (CaptureSource == SoundCaptureSources.SoundCard)
            {
                _soundCardSource.StopRecording();
            }

            _timer?.Stop();
        }

        void IRecognizer.Unpause()
        {
            if (CaptureSource == SoundCaptureSources.Microphone)
            {
                _micSource.StartRecording();
            }
            else if (CaptureSource == SoundCaptureSources.SoundCard)
            {
                _soundCardSource.StartRecording();
            }

            _timer.Start();
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;

            _micSource?.StopRecording();
            _micSource?.Dispose();
            _micSource = null;

            _soundCardSource?.StopRecording();
            _soundCardSource?.Dispose();
            _soundCardSource = null;

            _audio = null;
        }

        private void BuildDisposables()
        {
            _audio = new List<double>();

            //TODO: Timer doesn't work longterm. Speaking can happen over the timer elapse and
            // the message could be much less or more than the time alloted.
            // Need a way to trigger on mic picking up speech and mic no longer picking up speech.
            // Then the same for the sound card version.
            _timer = new Timer(10000)
            {
                Enabled = true,
                AutoReset = true
            };
            _timer.Elapsed += OnTimerElapsed;

            WaveFormat waveFormat = new WaveFormat(Convert.ToInt32(SAMPLE_RATE), bits: 16, channels: 1);
            _bytesPerSample = waveFormat.BitsPerSample / 8;

            if (CaptureSource == SoundCaptureSources.Microphone)
            {
                _micSource = new WaveInEvent
                {
                    WaveFormat = waveFormat,
                    BufferMilliseconds = 20
                };
            }
            else if (CaptureSource == SoundCaptureSources.SoundCard)
            {
                _soundCardSource = new WasapiLoopbackCapture()
                {
                    WaveFormat = waveFormat
                };
            }
        }

        private void OnNewAudioData(object s, WaveInEventArgs a)
        {
            // Inspired by https://github.com/nhannt201/VoiceNET.Library

            int newSampleCount = a.BytesRecorded / _bytesPerSample;
            double[] buffer = new double[newSampleCount];
            double peak = 0;
            for (int i = 0; i < newSampleCount; i++)
            {
                buffer[i] = BitConverter.ToInt16(a.Buffer, i * _bytesPerSample);
                peak = Math.Max(peak, buffer[i]);
            }
            lock (_audio)
            {
                _audio.AddRange(buffer);
            }
        }

        private double[] GetNewAudio()
        {
            lock (_audio)
            {
                double[] values = new double[_audio.Count];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = _audio[i];
                }
                _audio.RemoveRange(0, values.Length);
                return values;
            }
        }

        private void OnTimerElapsed(object s, ElapsedEventArgs e)
        {
            Recognize(GetNewAudio());
        }

        private void Recognize(double[] sourceBuffer)
        {
            // https://alphacephei.com/vosk/
            // https://github.com/alphacep/vosk-api/blob/master/csharp/demo/VoskDemo.cs

            if (_recognizer == null || _speechRecognizedAction == null) { return; }

            bool heard = false;
            StringBuilder resultsBuilder = new StringBuilder();

            int batch = 2048;
            int doublesRead = 0;
            while (doublesRead < sourceBuffer.Length)
            {
                List<float> fbuffer = new List<float>();
                for (int i = 0; i < batch && doublesRead != sourceBuffer.Length; i++, doublesRead++)
                {
                    fbuffer.Add((float)sourceBuffer[doublesRead]);
                }
                if (_recognizer.AcceptWaveform(fbuffer.ToArray(), fbuffer.Count))
                {
                    VoskRoot resultRoot = JsonSerializer.Deserialize<VoskRoot>(_recognizer.Result());
                    if (!string.IsNullOrEmpty(resultRoot?.Text))
                    {
                        heard = true;
                        resultsBuilder.Append(resultRoot.Text);
                    }
                }
            }

            VoskRoot finalResultRoot = JsonSerializer.Deserialize<VoskRoot>(_recognizer.FinalResult());
            if (!string.IsNullOrEmpty(finalResultRoot?.Text))
            {
                heard = true;
                resultsBuilder.Append(finalResultRoot.Text);
            }

            string phrase = resultsBuilder.ToString();
            if (heard && 
                phrase.Split(' ').Length > 1) // Sometimes silence or other little noices get interpreted as single words, like "huh" and "the".
            {
                _speechRecognizedAction(phrase);
            }
        }
    }
}
