using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Componants;
using AtaraxiaAI.Business.Services;
using Google.Cloud.TextToSpeech.V1;
using Google.Protobuf;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace AtaraxiaAI.Integrations.Services
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

        private readonly IntegrationDependencies _context;

        internal GoogleCloudSynthesizer(CultureInfo culture, IntegrationDependencies context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            if (_context.AppData.GoogleCloudSpeechToTextByteCount < FREE_LIMIT && CREDENTIALS_SET)
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

        bool ISynthesizer.IsAvailable() => _context.AppData.GoogleCloudSpeechToTextByteCount < FREE_LIMIT && CREDENTIALS_SET;

        async Task<byte[]> ISynthesizer.SynthesizeAsync(string message, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_context.AppData.GoogleCloudSpeechToTextByteCount + message.Length > FREE_LIMIT) return null;
            var input = new SynthesisInput { Text = message };
            var response = await _synthesizer.SynthesizeSpeechAsync(input, _voice, _audioConfig);
            cancellationToken.ThrowIfCancellationRequested();
            _context.AppData.GoogleCloudSpeechToTextByteCount += message.Length;
            return response.AudioContent.ToByteArray();
        }
    }
}
