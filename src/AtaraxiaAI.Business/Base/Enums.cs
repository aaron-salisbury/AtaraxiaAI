using System.ComponentModel.DataAnnotations;

namespace AtaraxiaAI.Business.Base;

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
        [Display(Name = "System")]
        SystemDotSpeech,

        [Display(Name = "Kokoro")]
        Kokoro
    }

    public enum VoiceGender
    {
        Female,
        Male
    }
}
