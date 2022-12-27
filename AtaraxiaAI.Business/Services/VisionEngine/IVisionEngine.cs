using System;

namespace AtaraxiaAI.Business.Services.VisionEngine
{
    public interface IVisionEngine
    {
        void Initiate(Action<byte[]> updateFrameAction);
    }
}
