using System;

namespace AtaraxiaAI.Business.Base
{
    public class Enums
    {
        public enum VisionCaptureSources
        {
            Webcam,
            Screen
        }

        public enum SoundCaptureSources
        {
            Microphone,
            SoundCard
        }

        public enum SpeechSynthesizers
        {
            Kokoro,
            GoogleCloud,
            MicrosoftAzure,
            MicrosoftBing,
            SystemDotSpeech
        }
    }
}
