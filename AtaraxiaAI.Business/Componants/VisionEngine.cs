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
            get { return _visionTask != null && _visionTask.Status == TaskStatus.Running; }
        }

        private IObjectDetector _objectDetector { get; set; }
        private Action<byte[]> _updateFrameAction;
        private CancellationTokenSource _visionTokenSource;
        private Task _visionTask;

        internal VisionEngine(Action<byte[]> updateFrameAction)
        {
            _updateFrameAction = updateFrameAction;
            _objectDetector = new YoloObjectDetector();
        }

        public void Activate()
        {
            AI.Log.Logger.Information("Beginning object detection.");

            Deactivate();
            _visionTokenSource = new CancellationTokenSource();
            _visionTask = Task.Run(() => _objectDetector.Initiate(_updateFrameAction, _visionTokenSource.Token));
        }

        public void Deactivate()
        {
            if (IsEngineRunning)
            {
                _visionTokenSource.Cancel();
                _visionTask.Wait();

                _visionTokenSource.Dispose();
                _visionTask.Dispose();

                AI.Log.Logger.Information("Ended object detection.");
            }
        }

        public void UpdateCaptureSource(VisionCaptureSources captureSource)
        {
            bool wasRunningWhenChangeMade = IsEngineRunning;

            if (wasRunningWhenChangeMade)
            {
                Deactivate();
            }

            if (_objectDetector is YoloObjectDetector yoloObjectDetector)
            {
                yoloObjectDetector.CaptureSource = captureSource;
            }

            if (wasRunningWhenChangeMade)
            {
                Activate();
            }
        }
    }
}
