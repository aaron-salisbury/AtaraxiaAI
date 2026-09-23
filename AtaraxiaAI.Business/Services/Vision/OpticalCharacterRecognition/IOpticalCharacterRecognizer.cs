using System;

namespace AtaraxiaAI.Business.Services
{
    public interface IOpticalCharacterRecognizer
    {
        string ReadTextFromImage(byte[] imageBuffer);
    }
}
