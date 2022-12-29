using AtaraxiaAI.Data.Domains;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtaraxiaAI.Business.Services
{
    /// <summary>
    /// Implementation of Microsoft Azure's Text-to-Speech API.
    /// https://azure.microsoft.com/en-us/products/cognitive-services/text-to-speech/
    /// To use, you need your own Azure subscription key.
    /// </summary>
    public class MicrosoftAzureSynthesizer : ISynthesizer
    {
        private const int FREE_LIMIT = 500000;
        private const bool CREDENTIALS_SET = false; //TODO: Flip when using real credentials.

        private SpeechConfig _speechConfig;

        public MicrosoftAzureSynthesizer(CultureInfo culture = null)
        {
            if (IsAvailable())
            {
                // Regions: https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/regions
                _speechConfig = SpeechConfig.FromSubscription("YourSubscriptionKey", "YourServiceRegion"); //TODO: Add your key and region.

                culture = culture ?? new CultureInfo("en-US");
                if (string.Equals(culture.Name, "en-US", StringComparison.OrdinalIgnoreCase))
                {
                    _speechConfig.SpeechSynthesisVoiceName = "en-US-JasonNeural";
                }
                else
                {
                    using (var synthesizer = new SpeechSynthesizer(_speechConfig))
                    using (var voicesResult = synthesizer.GetVoicesAsync().Result)
                    {
                        // Voices: https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support?tabs=stt-tts#prebuilt-neural-voices
                        _speechConfig.SpeechSynthesisVoiceName = voicesResult.Voices
                            .Where(v => v.Gender == SynthesisVoiceGender.Male && string.Equals(v.Locale, culture.Name, StringComparison.OrdinalIgnoreCase))
                            .First()
                            .ShortName;
                    }
                }
            }
        }

        public bool IsAvailable() => AI.AppData.MicrosoftAzureSpeechToTextCharCount < FREE_LIMIT && CREDENTIALS_SET;

        public async Task SpeakAsync(string message)
        {
            if (AI.AppData.MicrosoftAzureSpeechToTextCharCount + message.Length <= FREE_LIMIT)
            {
                try
                {
                    using (var synthesizer = new SpeechSynthesizer(_speechConfig))
                    using (var result = await synthesizer.SpeakTextAsync(message))
                    {
                        if (result.Reason == ResultReason.Canceled)
                        {
                            SpeechSynthesisCancellationDetails cancellation = SpeechSynthesisCancellationDetails.FromResult(result);

                            StringBuilder builder = new StringBuilder($"Failed to synthesize speech: {cancellation.Reason}");
                            builder.AppendLine($"ErrorCode={cancellation.ErrorCode}");
                            builder.Append($"ErrorDetails=[{cancellation.ErrorDetails}]");

                            AI.Log.Logger.Error(builder.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    AI.Log.Logger.Error($"Failed to synthesize speech: {e.Message}");
                }

                AI.AppData.MicrosoftAzureSpeechToTextCharCount += message.Length;
                await Data.CRUD.UpdateDataAsync<AppData>(AI.AppData, AI.Log.Logger);
            }
        }
    }
}
