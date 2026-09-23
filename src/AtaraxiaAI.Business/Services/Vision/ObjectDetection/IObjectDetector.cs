using System;
using System.Threading;

namespace AtaraxiaAI.Business.Services
{
    public interface IObjectDetector
    {
        AtaraxiaAI.Business.Base.Enums.VisionCaptureSources CaptureSource { get; set; }
        void Initiate(Action<byte[]> updateFrameAction, CancellationToken cancelToken);
    }
}
