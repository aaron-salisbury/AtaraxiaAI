using AtaraxiaAI.Business.Services;
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
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Integrations.Services;

internal class YoloObjectDetector : IObjectDetector
{
    private readonly IntegrationDependencies _dependencies;
    public VisionCaptureSources CaptureSource { get; set; }

    private readonly Net _net;
    private readonly string[] _classLabels;

    internal YoloObjectDetector(IntegrationDependencies dependencies, VisionCaptureSources captureSource = VisionCaptureSources.Screen)
    {
        _dependencies = dependencies;
        CaptureSource = captureSource;
        _classLabels = ModelAssets.CocoLabels;

        try
        {
            // Got files from https://pjreddie.com/darknet/yolo/
            // Using the "tiny" version since the regular weights file exceeds github file size limit.
            _net = DnnInvoke.ReadNetFromDarknet(ModelAssets.YoloConfiguration, ModelAssets.YoloWeights);
            _net.SetPreferableBackend(Emgu.CV.Dnn.Backend.OpenCV);
            _net.SetPreferableTarget(Target.Cpu);
        }
        catch (Exception e)
        {
            _dependencies.Logger.Error($"Failed to build neural net: {e.Message}");
        }
    }

    void IObjectDetector.Initiate(Action<byte[]> updateFrameAction, CancellationToken cancelToken)
    {
        _dependencies.Logger.Information("Initializing vision engine.");

        double? widthFactor = null;
        double? heightFactor = null;

        if (CaptureSource == VisionCaptureSources.Screen)
        {
            IScreenCaptureService screenCaptureService = new DX11ScreenCaptureService();
            IEnumerable<GraphicsCard> graphicsCards = screenCaptureService.GetGraphicsCards();
            IEnumerable<Display> displays = screenCaptureService.GetDisplays(graphicsCards.First());
            Display captureDisplay = displays.First();

            using IScreenCapture screenCapture = screenCaptureService.GetScreenCapture(captureDisplay);
            ICaptureZone captureZone = screenCapture.RegisterCaptureZone(0, 0, screenCapture.Display.Width, screenCapture.Display.Height);
            while (!cancelToken.IsCancellationRequested)
            {
                using Image<Bgra, byte> frame = GetScreenshotImage(screenCapture, captureZone);

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
            using Mat frame = new();
            using VideoCapture vc = new(0, VideoCapture.API.DShow);
            if (!vc.IsOpened)
            {
                _dependencies.Logger.Error("No webcam could be opened for vision capture.");
                return;
            }

            while (!cancelToken.IsCancellationRequested)
            {
                if (!vc.Read(frame) || frame.IsEmpty)
                {
                    continue;
                }

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

        // These wrap native OpenCV memory, released here rather than left to finalizers on every frame.
        using VectorOfMat output = new();
        using VectorOfRect boxes = new();
        using VectorOfFloat scores = new();
        using VectorOfInt indices = new();
        using Image<Bgr, byte> image = frame.ToImage<Bgr, byte>();
        using Mat input = DnnInvoke.BlobFromImage(image, 1 / 255.0, swapRB: true);

        _net.SetInput(input);

        _net.Forward(output, _net.UnconnectedOutLayersNames);

        for (int i = 0; i < output.Size; i++)
        {
            using Mat mat = output[i];
            float[,] data = (float[,])mat.GetData();

            for (int j = 0; j < data.GetLength(0); j++)
            {
                float[] row = [.. Enumerable.Range(0, data.GetLength(1)).Select(x => data[j, x])];

                float[] rowScore = [.. row.Skip(5)];
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

                    boxes.Push(new System.Drawing.Rectangle[] { new(x, y, boxWidth, boxHeight) });
                    indices.Push(new int[] { classId });
                    scores.Push(new float[] { confidence });
                }
            }
        }
        int[] bestIndex = DnnInvoke.NMSBoxes(boxes.ToArray(), scores.ToArray(), .8f, .8f);

        using Image<Bgr, byte> frameOut = frame.ToImage<Bgr, byte>();

        for (int i = 0; i < bestIndex.Length; i++)
        {
            int index = bestIndex[i];
            System.Drawing.Rectangle box = boxes[index];
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

    // Capture already runs on the vision engine's background task, so this works synchronously.
    private static Image<Bgra, byte> GetScreenshotImage(IScreenCapture screenCapture, ICaptureZone captureZone)
    {
        screenCapture.CaptureScreen();

        using IDisposable captureLock = captureZone.Lock();
        byte[] buffer = captureZone.RawBuffer.ToArray();

        GCHandle pinnedArray = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            using Image<Bgra, byte> cvImage = new(captureZone.Width, captureZone.Height,
                captureZone.Stride, pinnedArray.AddrOfPinnedObject());
            // The image constructor retains the pointer. Copy while it is still pinned.
            return cvImage.Copy();
        }
        finally
        {
            pinnedArray.Free();
        }
    }
}
