using System;

namespace AtaraxiaAI.Business.Services
{
    public interface IVisionEngine
    {
        void Initiate(Action<byte[]> updateFrameAction);
    }
}
