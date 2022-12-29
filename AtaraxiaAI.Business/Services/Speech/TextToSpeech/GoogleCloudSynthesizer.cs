using AtaraxiaAI.Data.Domains;
using Google.Cloud.TextToSpeech.V1;
using Google.Protobuf;
using NAudio.Wave;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    /// <summary>
    /// Implementation of Google's Text-to-Speech API.
    /// https://cloud.google.com/text-to-speech/
    /// To use, you need your own Google Cloud project that has the following:
    ///     Text-to-Speech API enabled.
    ///     A Service Account for credential authetication.
    /// See https://codelabs.developers.google.com/codelabs/cloud-text-speech-csharp/
    /// </summary>
    public class GoogleCloudSynthesizer : ISynthesizer
    {
        private const int FREE_LIMIT = 1000000;
        private const bool CREDENTIALS_SET = false; //TODO: Flip when using real credentials.

        private TextToSpeechClient _synthesizer;
        private AudioConfig _audioConfig;
        private VoiceSelectionParams _voice;

        public GoogleCloudSynthesizer(CultureInfo culture = null)
        {
            if (IsAvailable())
            {
                culture = culture ?? new CultureInfo("en-US");
                _audioConfig = new AudioConfig { AudioEncoding = AudioEncoding.Mp3 };

                _voice = new VoiceSelectionParams
                {
                    LanguageCode = culture.Name,
                    SsmlGender = SsmlVoiceGender.Male,
                    Name = string.Equals(culture.Name, "en-US", StringComparison.OrdinalIgnoreCase) ? "en-US-Neural2-J" : null
                };

                // Credentials: https://cloud.google.com/docs/authentication/provide-credentials-adc#local-dev
                _synthesizer = new TextToSpeechClientBuilder().Build(); //TODO: Set your credentials.
            }
        }

        public bool IsAvailable() => AI.AppData.GoogleCloudSpeechToTextByteCount < FREE_LIMIT && CREDENTIALS_SET;

        public async Task SpeakAsync(string message)
        {
            if (AI.AppData.GoogleCloudSpeechToTextByteCount + message.Length <= FREE_LIMIT)
            {
                SynthesisInput input = new SynthesisInput { Text = message };
                SynthesizeSpeechResponse response = await _synthesizer.SynthesizeSpeechAsync(input, _voice, _audioConfig);

                try
                {
                    using (var ms = new MemoryStream(response.AudioContent.ToByteArray()))
                    using (var rdr = new Mp3FileReader(ms))
                    using (var wavStream = WaveFormatConversionStream.CreatePcmStream(rdr))
                    using (var baStream = new BlockAlignReductionStream(wavStream))
                    using (var waveOut = new WaveOutEvent())
                    {
                        waveOut.Init(baStream);
                        waveOut.Play();
                        while (waveOut.PlaybackState == PlaybackState.Playing)
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
                catch (Exception e)
                {
                    AI.Log.Logger.Error($"Failed to synthesize speech: {e.Message}");
                }

                AI.AppData.GoogleCloudSpeechToTextByteCount += input.ToByteArray().Length;
                await Data.CRUD.UpdateDataAsync<AppData>(AI.AppData, AI.Log.Logger);
            }
        }
    }
}
