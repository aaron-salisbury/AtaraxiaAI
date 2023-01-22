using AtaraxiaAI.Data;
using Emgu.CV;
using Emgu.CV.Dnn;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ScreenCapture.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Business.Services
{
    internal class YoloObjectDetector : IObjectDetector
    {
        internal VisionCaptureSources CaptureSource { get; set; }

        private Net _net;
        private string[] _classLabels;

        internal YoloObjectDetector(VisionCaptureSources captureSource = VisionCaptureSources.Screen)
        {
            CaptureSource = captureSource;
            _classLabels = CRUD.ReadCOCOClassLabels();

            try
            {
                // Got files from https://pjreddie.com/darknet/yolo/
                // Using the "tiny" version since the regular weights file exceeds github file size limit.
                _net = DnnInvoke.ReadNetFromDarknet(CRUD.ReadYoloCFGBuffer(), CRUD.ReadYoloWeightsBuffer());
                _net.SetPreferableBackend(Emgu.CV.Dnn.Backend.OpenCV);
                _net.SetPreferableTarget(Target.Cpu);
            }
            catch (Exception e)
            {
                AI.Logger.Error($"Failed to build neural net: {e.Message}");
            }
        }

        void IObjectDetector.Initiate(Action<byte[]> updateFrameAction, CancellationToken cancelToken)
        {
            AI.Logger.Information("Initializing vision engine.");

            double? widthFactor = null;
            double? heightFactor = null;

            if (CaptureSource == VisionCaptureSources.Screen)
            {
                IScreenCaptureService screenCaptureService = new DX11ScreenCaptureService();
                IEnumerable<GraphicsCard> graphicsCards = screenCaptureService.GetGraphicsCards();
                IEnumerable<Display> displays = screenCaptureService.GetDisplays(graphicsCards.First());
                Display captureDisplay = displays.First();

                using IScreenCapture screenCapture = screenCaptureService.GetScreenCapture(captureDisplay);
                CaptureZone captureZone = screenCapture.RegisterCaptureZone(0, 0, screenCapture.Display.Width, screenCapture.Display.Height);
                while (!cancelToken.IsCancellationRequested)
                {
                    Image<Bgra, byte> frame = GetScreenshotImage(screenCapture, captureZone).Result;

                    if (widthFactor == null || heightFactor == null)
                    {
                        widthFactor = GetFactor(frame.Mat.Width);
                        heightFactor = GetFactor(frame.Mat.Height);
                    }

                    updateFrameAction(ProcessFrame(frame.Mat, widthFactor.Value, heightFactor.Value));
                }
            }
            else if (CaptureSource == VisionCaptureSources.Webcam)
            {
                Mat frame = new Mat();

                using VideoCapture vc = new VideoCapture(0, VideoCapture.API.DShow);
                while (!cancelToken.IsCancellationRequested)
                {
                    vc.Read(frame);

                    if (widthFactor == null || heightFactor == null)
                    {
                        widthFactor = GetFactor(frame.Width);
                        heightFactor = GetFactor(frame.Height);
                    }

                    updateFrameAction(ProcessFrame(frame, widthFactor.Value, heightFactor.Value));
                }
            }
        }

        // Scale factor must result in dimensions where 32 is a multiple.
        private double GetFactor(int dimensionValue)
        {
            const double defaultFactor = 0.4d;

            if (dimensionValue * defaultFactor % 32 == 0)
            {
                return defaultFactor;
            }
            else
            {
                double result = (int)(dimensionValue * defaultFactor / 32);
                return result * 32.0d / dimensionValue;
            }
        }

        // Inspired by https://youtu.be/v7_g1Zoapkg?t=403
        private byte[] ProcessFrame(Mat frame, double widthFactor, double heightFactor)
        {
            CvInvoke.Resize(frame, frame, new System.Drawing.Size(0, 0), widthFactor, heightFactor);

            VectorOfMat output = new VectorOfMat();
            VectorOfRect boxes = new VectorOfRect();
            VectorOfFloat scores = new VectorOfFloat();
            VectorOfInt indices = new VectorOfInt();

            Image<Bgr, byte> image = frame.ToImage<Bgr, byte>();

            Mat input = DnnInvoke.BlobFromImage(image, 1 / 255.0, swapRB: true);

            _net.SetInput(input);

            _net.Forward(output, _net.UnconnectedOutLayersNames);

            for (int i = 0; i < output.Size; i++)
            {
                var mat = output[i];
                float[,] data = (float[,])mat.GetData();

                for (int j = 0; j < data.GetLength(0); j++)
                {
                    float[] row = Enumerable.Range(0, data.GetLength(1))
                                  .Select(x => data[j, x])
                                  .ToArray();

                    float[] rowScore = row.Skip(5).ToArray();
                    int classId = rowScore.ToList().IndexOf(rowScore.Max());
                    float confidence = rowScore[classId];

                    if (confidence > 0.8f)
                    {
                        int centerX = (int)(row[0] * frame.Width);
                        int centerY = (int)(row[1] * frame.Height);
                        int boxWidth = (int)(row[2] * frame.Width);
                        int boxHeight = (int)(row[3] * frame.Height);

                        int x = centerX - boxWidth / 2;
                        int y = centerY - boxHeight / 2;

                        boxes.Push(new System.Drawing.Rectangle[] { new System.Drawing.Rectangle(x, y, boxWidth, boxHeight) });
                        indices.Push(new int[] { classId });
                        scores.Push(new float[] { confidence });
                    }
                }
            }
            int[] bestIndex = DnnInvoke.NMSBoxes(boxes.ToArray(), scores.ToArray(), .8f, .8f);

            Image<Bgr, byte> frameOut = frame.ToImage<Bgr, byte>();

            for (int i = 0; i < bestIndex.Length; i++)
            {
                int index = bestIndex[i];
                var box = boxes[index];
                CvInvoke.Rectangle(frameOut, box, new MCvScalar(255, 255, 255), 1);
                CvInvoke.PutText(
                    frameOut,
                    _classLabels[indices[index]],
                    new System.Drawing.Point(box.X, box.Y - 20),
                    Emgu.CV.CvEnum.FontFace.HersheyPlain,
                    1.0,
                    new MCvScalar(255, 255, 255), 2);
            }

            CvInvoke.Resize(frameOut, frameOut, new System.Drawing.Size(0, 0), 4, 4);

            return frameOut.ToJpegData();
        }

        private static Task<Image<Bgra, byte>> GetScreenshotImage(IScreenCapture screenCapture, CaptureZone captureZone)
        {
            return Task.Run(() =>
            {
                screenCapture.CaptureScreen();

                lock (captureZone.Buffer)
                {
                    GCHandle pinnedArray = GCHandle.Alloc(captureZone.Buffer, GCHandleType.Pinned);
                    IntPtr pointer = pinnedArray.AddrOfPinnedObject();

                    Image<Bgra, byte> cvImage = new Image<Bgra, byte>(captureZone.Width, captureZone.Height, captureZone.Stride, pointer);

                    pinnedArray.Free();

                    return cvImage;
                }
            });
        }
    }
}
