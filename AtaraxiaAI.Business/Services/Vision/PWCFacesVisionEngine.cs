﻿using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.Threading;

namespace AtaraxiaAI.Business.Services
{
    // https://youtu.be/YTBAjP-0Fto
    internal class PWCFacesVisionEngine : IVisionEngine
    {
        private CascadeClassifier _faceCascade;

        internal PWCFacesVisionEngine()
        {
            _faceCascade = new CascadeClassifier("./Detection/PWCVision/haarcascade_frontalface_default.xml");
        }

        void IVisionEngine.Initiate(Action<byte[]> updateFrameAction, CancellationToken cancelToken)
        {
            AI.Log.Logger.Information("Initializing vision engine.");

            Mat frame = new Mat();
            Mat frameGray = new Mat();

            using (VideoCapture vc = new VideoCapture(0, VideoCapture.API.DShow))
            {
                while (!cancelToken.IsCancellationRequested)
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
    }
}
