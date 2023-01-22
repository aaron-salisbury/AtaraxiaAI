using AtaraxiaAI.Data;
using System;
using Tesseract;

namespace AtaraxiaAI.Business.Services
{
    internal class TesseractOCR : IOpticalCharacterRecognizer
    {
        public string ReadTextFromImage(byte[] imageBuffer)
        {
            string text = null;
            
            try
            {
                using (TesseractEngine engine = new TesseractEngine(CRUD.ReadTessdataContentPath(), "eng", EngineMode.Default))
                using (Pix image = Pix.LoadFromMemory(imageBuffer))
                //using (Pix image = Pix.LoadFromFile("./Detection/Vision/OCR/tessdata/test.jpg"))
                using (Page page = engine.Process(image))
                {
                    text = page.GetText().TrimEnd();
                }
            }
            catch (Exception e)
            {
                AI.Logger.Error($"Failed to parse text from image: {e.Message}");
            }

            return text;
        }
    }
}
