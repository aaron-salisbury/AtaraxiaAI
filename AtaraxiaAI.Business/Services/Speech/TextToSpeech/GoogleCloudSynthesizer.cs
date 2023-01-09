using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Data.Domains;
using Google.Cloud.TextToSpeech.V1;
using Google.Protobuf;
using System;
using System.Globalization;
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
    internal class GoogleCloudSynthesizer : ISynthesizer
    {
        private const int FREE_LIMIT = 1000000;
        private const bool CREDENTIALS_SET = false; //TODO: Flip when using real credentials.

        private TextToSpeechClient _synthesizer;
        private AudioConfig _audioConfig;
        private VoiceSelectionParams _voice;

        internal GoogleCloudSynthesizer(CultureInfo culture = null)
        {
            if (AI.AppData.GoogleCloudSpeechToTextByteCount < FREE_LIMIT && CREDENTIALS_SET)
            {
                culture = culture ?? new CultureInfo("en-US");
                _audioConfig = new AudioConfig { AudioEncoding = AudioEncoding.Linear16 };

                _voice = new VoiceSelectionParams
                {
                    LanguageCode = culture.Name,
                    SsmlGender = SsmlVoiceGender.Female,
                    Name = string.Equals(culture.Name, "en-US", StringComparison.OrdinalIgnoreCase) ? "en-US-Neural2-F" : null
                };

                // Credentials: https://cloud.google.com/docs/authentication/provide-credentials-adc#local-dev
                _synthesizer = new TextToSpeechClientBuilder().Build(); //TODO: Set your credentials.
            }
        }

        bool ISynthesizer.IsAvailable() => AI.AppData.GoogleCloudSpeechToTextByteCount < FREE_LIMIT && CREDENTIALS_SET;

        async Task<bool> ISynthesizer.SpeakAsync(string message)
        {
            bool isSuccessful = false;

            if (AI.AppData.GoogleCloudSpeechToTextByteCount + message.Length <= FREE_LIMIT)
            {
                SynthesisInput input = new SynthesisInput { Text = message };
                SynthesizeSpeechResponse response = await _synthesizer.SynthesizeSpeechAsync(input, _voice, _audioConfig);

                try
                {
                    SpeechEngine.StreamSpeechToSpeaker(response.AudioContent.ToByteArray(), message);
                    isSuccessful = true;
                }
                catch (Exception e)
                {
                    AI.Log.Logger.Error($"Failed to synthesize speech: {e.Message}");
                }

                AI.AppData.GoogleCloudSpeechToTextByteCount += input.ToByteArray().Length;
            }
            else
            {
                // If we're this close to the limit, just max it out and don't bother to try again until next month.
                AI.AppData.GoogleCloudSpeechToTextByteCount = FREE_LIMIT;
            }

            return isSuccessful;
        }
    }
}
