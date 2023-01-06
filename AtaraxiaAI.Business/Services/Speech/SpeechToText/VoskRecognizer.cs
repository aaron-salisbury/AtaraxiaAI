using AtaraxiaAI.Business.Services.Base.Domains;
using AtaraxiaAI.Data.Base;
using System;
using System.IO;
using System.Text;
using Vosk;

namespace AtaraxiaAI.Business.Services
{
    // https://alphacephei.com/vosk/
    // https://github.com/alphacep/vosk-api/blob/master/csharp/demo/VoskDemo.cs
    internal class VoskRecognizer : IRecognizer
    {
        private Vosk.VoskRecognizer _recognizer;

        internal VoskRecognizer()
        {
            Vosk.Vosk.SetLogLevel(-1);

            Vosk.VoskRecognizer _recognizer = new Vosk.VoskRecognizer(
                new Model(Data.CRUD.ReadVoskModelContentPath()), 
                16000.0f);

            _recognizer.SetMaxAlternatives(0);
            _recognizer.SetWords(true);



            // Demo byte buffer
            StringBuilder recognizedWordsBuilder = new StringBuilder();

            using (Stream source = File.OpenRead("./Detection/Voice/Vosk/test.wav"))
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (_recognizer.AcceptWaveform(buffer, bytesRead))
                    {
                        VoskResultRoot result = Json.ToObjectAsync<VoskResultRoot>(_recognizer.Result()).Result;
                        recognizedWordsBuilder.Append(result.Text);
                        recognizedWordsBuilder.Append(' ');
                    }
                }
            }

            VoskResultRoot finalResult = Json.ToObjectAsync<VoskResultRoot>(_recognizer.FinalResult()).Result;
            recognizedWordsBuilder.Append(finalResult.Text);

            AI.Log.Logger.Information($"*Heard* \"{recognizedWordsBuilder}\"");
        }

        bool IRecognizer.IsAvailable()
        {
            throw new NotImplementedException();
        }

        void IRecognizer.Listen(Action<string> speechRecognizedAction)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
