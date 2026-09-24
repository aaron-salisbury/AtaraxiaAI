using AtaraxiaAI.Business.Services;
using Serilog;
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

        private readonly ILogger _logger;
        private IObjectDetector _objectDetector;
        private IOpticalCharacterRecognizer _ocRecognizer;
        private Action<byte[]> _updateFrameAction;
        private CancellationTokenSource? _visionTokenSource;
        private Task? _visionTask;

        internal VisionEngine(Action<byte[]> updateFrameAction, IIntegrationFactory integrations, ILogger logger)
        {
            _updateFrameAction = updateFrameAction;
            _logger = logger;
            _objectDetector = integrations.CreateObjectDetector();
            _ocRecognizer = integrations.CreateOpticalCharacterRecognizer();
        }

        public void Activate()
        {
            _logger.Information("Beginning object detection.");

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
                _logger.Error(ex, "Vision capture failed while stopping.");
            }
            finally
            {
                task.Dispose();
                cancellation.Dispose();
                _visionTask = null;
                _visionTokenSource = null;
                _logger.Information("Ended object detection.");
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
