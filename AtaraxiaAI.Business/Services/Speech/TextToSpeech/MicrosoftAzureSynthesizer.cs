using AtaraxiaAI.Business.Base;
using AtaraxiaAI.Business.Componants;
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
    internal class MicrosoftAzureSynthesizer : ISynthesizer
    {
        private const string API_KEY = null; //TODO: Apply your own key.
        private const string REGION = null; //TODO: Your own subscription project region. https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/regions
        private const int FREE_LIMIT = 500000;

        private SpeechConfig _speechConfig;

        internal MicrosoftAzureSynthesizer(CultureInfo culture = null)
        {
            if (AI.AppData.MicrosoftAzureSpeechToTextCharCount < FREE_LIMIT && AreCredentialsSet())
            {
                _speechConfig = SpeechConfig.FromSubscription(API_KEY, REGION);

                culture = culture ?? new CultureInfo("en-US");
                if (string.Equals(culture.Name, "en-US", StringComparison.OrdinalIgnoreCase))
                {
                    _speechConfig.SpeechSynthesisVoiceName = "en-US-CoraNeural";
                }
                else
                {
                    using (var synthesizer = new SpeechSynthesizer(_speechConfig))
                    using (var voicesResult = synthesizer.GetVoicesAsync().Result)
                    {
                        // Voices: https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support?tabs=stt-tts#prebuilt-neural-voices
                        _speechConfig.SpeechSynthesisVoiceName = voicesResult.Voices
                            .Where(v => v.Gender == SynthesisVoiceGender.Female && string.Equals(v.Locale, culture.Name, StringComparison.OrdinalIgnoreCase))
                            .First()
                            .ShortName;
                    }
                }
            }
        }

        private bool AreCredentialsSet() => !string.IsNullOrEmpty(API_KEY) && !string.IsNullOrEmpty(REGION);

        bool ISynthesizer.IsAvailable() => AI.AppData.MicrosoftAzureSpeechToTextCharCount < FREE_LIMIT && AreCredentialsSet();

        async Task<bool> ISynthesizer.SpeakAsync(string message)
        {
            bool isSuccessful = false;

            if (AI.AppData.MicrosoftAzureSpeechToTextCharCount + message.Length <= FREE_LIMIT)
            {
                try
                {
                    using (var synthesizer = new SpeechSynthesizer(_speechConfig, null))
                    using (var result = await synthesizer.SpeakTextAsync(message))
                    {
                        if (result.Reason == ResultReason.Canceled)
                        {
                            SpeechSynthesisCancellationDetails cancellation = SpeechSynthesisCancellationDetails.FromResult(result);

                            StringBuilder builder = new StringBuilder($"Failed to synthesize speech: {cancellation.Reason}");
                            builder.AppendLine($"{InMemoryLogger.NEW_LINE_PREFIX}ErrorCode={cancellation.ErrorCode}");
                            builder.Append($"{InMemoryLogger.NEW_LINE_PREFIX}ErrorDetails=[{cancellation.ErrorDetails}]");

                            AI.Log.Logger.Error(builder.ToString());
                        }
                        else
                        {
                            SpeechEngine.StreamSpeechToSpeaker(result.AudioData, message);
                            isSuccessful = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    AI.Log.Logger.Error($"Failed to synthesize speech: {e.Message}");
                }

                AI.AppData.MicrosoftAzureSpeechToTextCharCount += message.Length;
                await Data.CRUD.UpdateDataAsync<AppData>(AI.AppData, AI.InternalStorage.UserStorageDirectory, AI.Log.Logger);
            }
            else
            {
                // If we're this close to the limit, just max it out and don't bother to try again until next month.
                AI.AppData.MicrosoftAzureSpeechToTextCharCount = FREE_LIMIT;
                await Data.CRUD.UpdateDataAsync<AppData>(AI.AppData, AI.InternalStorage.UserStorageDirectory, AI.Log.Logger);
            }

            return isSuccessful;
        }
    }
}
