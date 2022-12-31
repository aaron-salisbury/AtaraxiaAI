using System;

namespace AtaraxiaAI.Business.Services
{
    public interface IVisionEngine
    {
        bool IsActive();

        void Initiate(Action<byte[]> updateFrameAction);

        void Deactivate();
    }
}
