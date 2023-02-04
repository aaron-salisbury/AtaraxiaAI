using System;

namespace AtaraxiaAI.Business.Services
{
    internal interface IOpticalCharacterRecognizer
    {
        string ReadTextFromImage(byte[] imageBuffer);
    }
}
