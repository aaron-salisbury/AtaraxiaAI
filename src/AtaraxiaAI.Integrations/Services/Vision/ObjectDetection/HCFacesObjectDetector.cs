using AtaraxiaAI.Business;
using AtaraxiaAI.Business.Services;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.Threading;

namespace AtaraxiaAI.Integrations.Services
{
    // Inspired by https://youtu.be/v7_g1Zoapkg?t=50
    internal class HCFacesObjectDetector : IObjectDetector
    {
        private readonly IntegrationDependencies _dependencies;
        public AtaraxiaAI.Business.Base.Enums.VisionCaptureSources CaptureSource { get; set; }

        private CascadeClassifier _faceCascade;

        internal HCFacesObjectDetector(IntegrationDependencies dependencies)
        {
            _dependencies = dependencies;
            _faceCascade = new CascadeClassifier(ModelAssets.HaarCascadePath);
        }

        void IObjectDetector.Initiate(Action<byte[]> updateFrameAction, CancellationToken cancelToken)
        {
            _dependencies.Logger.Information("Initializing vision engine.");

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
