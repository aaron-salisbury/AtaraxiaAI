using System;
using System.IO;
using Vosk;

namespace AtaraxiaAI.Business.Services
{
    // https://alphacephei.com/vosk/
    // https://github.com/alphacep/vosk-api/blob/master/csharp/demo/VoskDemo.cs
    internal class VoskRecognizer : IRecognizer
    {
        internal VoskRecognizer()
        {
            Vosk.Vosk.SetLogLevel(-1);
            Model model = new Model(Data.CRUD.ReadVoskModelContentPath());

            // Demo byte buffer
            Vosk.VoskRecognizer rec = new Vosk.VoskRecognizer(model, 16000.0f);
            rec.SetMaxAlternatives(0);
            rec.SetWords(true);
            using (Stream source = File.OpenRead("./Detection/Voice/Vosk/test.wav"))
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (rec.AcceptWaveform(buffer, bytesRead))
                    {
                        AI.Log.Logger.Information($"RESULT: {rec.Result()}");
                    }
                    else
                    {
                        AI.Log.Logger.Information($"PARTIAL RESULT: {rec.PartialResult()}");
                    }
                }
            }

            AI.Log.Logger.Information($"FINAL RESULT: {rec.FinalResult()}");
        }

        bool IRecognizer.IsAvailable()
        {
            throw new NotImplementedException();
        }

        void IRecognizer.Listen(Action<string> speechRecognizedAction)
        {
            throw new NotImplementedException();
        }

        void IRecognizer.Shutdown()
        {
            throw new NotImplementedException();
        }
    }
}
