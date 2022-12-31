using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;

namespace AtaraxiaAI.Business.Services
{
    // https://youtu.be/YTBAjP-0Fto
    internal class PWCFacesVisionEngine : IVisionEngine
    {
        public bool IsRunning { get; set; }

        private CascadeClassifier _faceCascade;

        public PWCFacesVisionEngine()
        {
            _faceCascade = new CascadeClassifier("./Detection/PWCVision/haarcascade_frontalface_default.xml");
        }

        public bool IsActive() => IsRunning;

        public void Initiate(Action<byte[]> updateFrameAction)
        {
            AI.Log.Logger.Information("Initializing vision engine.");

            Mat frame = new Mat();
            Mat frameGray = new Mat();

            IsRunning = true;
            using (VideoCapture vc = new VideoCapture(0, VideoCapture.API.DShow))
            {
                while (IsRunning)
                {
                    vc.Read(frame);

                    CvInvoke.CvtColor(frame, frameGray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

                    Rectangle[] faces = _faceCascade.DetectMultiScale(frameGray, 1.3, 5);

                    if (faces != null && faces.Length > 0)
                    {
                        CvInvoke.Rectangle(frame, faces[0], new MCvScalar(0, 255, 0), 2);
                    }

                    Image<Bgr, byte> frameImage = frame.ToImage<Bgr, byte>();
                    updateFrameAction(frameImage.ToJpegData());
                }
            }
            
        }

        public void Deactivate()
        {
            IsRunning = false;
        }
    }
}
