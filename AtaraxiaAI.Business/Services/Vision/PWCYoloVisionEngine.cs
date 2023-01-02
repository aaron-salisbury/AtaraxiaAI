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

namespace AtaraxiaAI.Business.Services
{
    // https://youtu.be/v7_g1Zoapkg
    internal class PWCYoloVisionEngine : IVisionEngine
    {
        public enum CaptureSources
        {
            Webcam,
            Screen
        }

        private CaptureSources _captureSource;
        private Net _net;
        private string[] _classLabels;
        private IScreenCaptureService _screenCaptureService;
        private Display _captureDisplay;

        public PWCYoloVisionEngine(CaptureSources captureSource = CaptureSources.Screen)
        {
            _captureSource = captureSource;
            _classLabels = CRUD.ReadCOCOClassLabels();

            _screenCaptureService = new DX11ScreenCaptureService();
            IEnumerable<GraphicsCard> graphicsCards = _screenCaptureService.GetGraphicsCards();
            IEnumerable<Display> displays = _screenCaptureService.GetDisplays(graphicsCards.First());
            _captureDisplay = displays.First();

            try
            {
                // Got files from https://pjreddie.com/darknet/yolo/
                // Using the "tiny" version since the regular weights file exceeds github file size limit.
                _net = DnnInvoke.ReadNetFromDarknet(CRUD.ReadYoloCFGBuffer(), CRUD.ReadYoloWeightsBuffer());
                _net.SetPreferableBackend(Emgu.CV.Dnn.Backend.OpenCV);
                _net.SetPreferableTarget(Emgu.CV.Dnn.Target.Cpu);
            }
            catch (Exception e)
            {
                AI.Log.Logger.Error($"Failed to build neural net: {e.Message}");
            }
        }

        public void Initiate(Action<byte[]> updateFrameAction, CancellationToken cancelToken)
        {
            AI.Log.Logger.Information("Initializing vision engine.");

            if (_captureSource == CaptureSources.Screen)
            {
                using IScreenCapture screenCapture = _screenCaptureService.GetScreenCapture(_captureDisplay);
                CaptureZone captureZone = screenCapture.RegisterCaptureZone(0, 0, screenCapture.Display.Width, screenCapture.Display.Height);
                while (!cancelToken.IsCancellationRequested)
                {
                    Image<Bgra, byte> frame = GetScreenImage(screenCapture, captureZone).Result;

                    updateFrameAction(ProcessFrame(frame.Mat).ToJpegData());
                }
            }
            else if (_captureSource == CaptureSources.Webcam)
            {
                Mat frame = new Mat();

                using VideoCapture vc = new VideoCapture(0, VideoCapture.API.DShow);
                while (!cancelToken.IsCancellationRequested)
                {
                    vc.Read(frame);

                    updateFrameAction(ProcessFrame(frame).ToJpegData());
                }
            }
        }

        private Image<Bgr, byte> ProcessFrame(Mat frame)
        {
            CvInvoke.Resize(frame, frame, new System.Drawing.Size(0, 0), .4, .4);

            VectorOfMat output = new VectorOfMat(); // TODO: This was never getting newed?
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

                        int x = (int)(centerX - (boxWidth / 2));
                        int y = (int)(centerY - (boxHeight / 2));

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

            return frameOut;
        }

        private static Task<Image<Bgra, byte>> GetScreenImage(IScreenCapture screenCapture, CaptureZone captureZone)
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
