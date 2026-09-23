using AtaraxiaAI.Business.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using static AtaraxiaAI.Business.Base.Enums;

namespace AtaraxiaAI.Business.Componants
{
    public class VisionEngine
    {
        public bool IsEngineRunning
        {
            get { return _visionTask is { IsCompleted: false }; }
        }

        private IObjectDetector _objectDetector;
        private IOpticalCharacterRecognizer _ocRecognizer;
        private Action<byte[]> _updateFrameAction;
        private CancellationTokenSource? _visionTokenSource;
        private Task? _visionTask;

        internal VisionEngine(Action<byte[]> updateFrameAction)
        {
            _updateFrameAction = updateFrameAction;
            _objectDetector = AI.Integrations.CreateObjectDetector();
            _ocRecognizer = AI.Integrations.CreateOpticalCharacterRecognizer();
        }

        public void Activate()
        {
            AI.Logger.Information("Beginning object detection.");

            Deactivate();
            CancellationTokenSource cancellation = new CancellationTokenSource();
            _visionTokenSource = cancellation;
            _visionTask = Task.Run(() => _objectDetector.Initiate(_updateFrameAction, cancellation.Token));
        }

        public void Deactivate()
        {
            Task? task = _visionTask;
            CancellationTokenSource? cancellation = _visionTokenSource;
            if (task is null || cancellation is null) return;

            cancellation.Cancel();
            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Cancellation is the expected result when stopping capture.
            }
            catch (Exception ex)
            {
                AI.Logger.Error(ex, "Vision capture failed while stopping.");
            }
            finally
            {
                task.Dispose();
                cancellation.Dispose();
                _visionTask = null;
                _visionTokenSource = null;
                AI.Logger.Information("Ended object detection.");
            }
        }

        public void UpdateCaptureSource(VisionCaptureSources captureSource)
        {
            bool wasRunningWhenChangeMade = IsEngineRunning;

            if (wasRunningWhenChangeMade)
            {
                Deactivate();
            }

            _objectDetector.CaptureSource = captureSource;

            if (wasRunningWhenChangeMade)
            {
                Activate();
            }
        }

        internal string ReadTextFromImage(byte[] imageBuffer)
        {
            return _ocRecognizer.ReadTextFromImage(imageBuffer);
        }
    }
}
