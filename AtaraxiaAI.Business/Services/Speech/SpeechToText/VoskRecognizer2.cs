using AtaraxiaAI.Business.Services.Base.Domains;
using AtaraxiaAI.Data.Base;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using Vosk;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Business.Services
{
    internal class VoskRecognizer2 : IRecognizer
    {
        private const float SAMPLE_RATE = 16000f;

        internal SoundCaptureSources CaptureSource { get; set; }

        private WaveInEvent _micSource;
        private WasapiCapture _soundCardSource;
        private List<double> _audio;
        private Timer _timer;
        private Action<string> _speechRecognizedAction;
        private Vosk.VoskRecognizer _recognizer;

        internal VoskRecognizer2(SoundCaptureSources captureSource = SoundCaptureSources.SoundCard)
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

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _micSource?.StopRecording();
            _micSource?.Dispose();
            _soundCardSource?.StopRecording();
            _soundCardSource?.Dispose();

            _audio = null;
            _timer = null;
            _micSource = null;
            _soundCardSource = null;
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

            _micSource = new WaveInEvent
            {
                WaveFormat = new WaveFormat(Convert.ToInt32(SAMPLE_RATE), bits: 16, channels: 1),
                BufferMilliseconds = 20
            };

            _soundCardSource = new WasapiLoopbackCapture()
            {
                WaveFormat = new WaveFormat(Convert.ToInt32(SAMPLE_RATE), bits: 16, channels: 1)
            };
        }

        // Inspired by https://github.com/nhannt201/VoiceNET.Library
        private void OnNewAudioData(object s, WaveInEventArgs a)
        {
            int bytesPerSample = _micSource.WaveFormat.BitsPerSample / 8;
            int newSampleCount = a.BytesRecorded / bytesPerSample;
            double[] buffer = new double[newSampleCount];
            double peak = 0;
            for (int i = 0; i < newSampleCount; i++)
            {
                buffer[i] = BitConverter.ToInt16(a.Buffer, i * bytesPerSample);
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
            const string silenceRecognizedAs = "huh"; // For some reason this happens.

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
                    VoskRoot resultRoot = Json.ToObjectAsync<VoskRoot>(_recognizer.Result()).Result;
                    if (!string.IsNullOrEmpty(resultRoot?.Text))
                    {
                        heard = true;
                        resultsBuilder.Append(resultRoot.Text);
                    }
                }
            }

            VoskRoot finalResultRoot = Json.ToObjectAsync<VoskRoot>(_recognizer.FinalResult()).Result;
            if (!string.IsNullOrEmpty(finalResultRoot?.Text) && !string.Equals(silenceRecognizedAs, finalResultRoot.Text))
            {
                heard = true;
                resultsBuilder.Append(finalResultRoot.Text);
            }

            if (heard)
            {
                _speechRecognizedAction(resultsBuilder.ToString());
            }
        }
    }
}
