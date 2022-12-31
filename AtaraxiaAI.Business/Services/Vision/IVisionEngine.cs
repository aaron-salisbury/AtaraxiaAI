using System;
using System.Threading;

namespace AtaraxiaAI.Business.Services
{
    public interface IVisionEngine
    {
        void Initiate(Action<byte[]> updateFrameAction, CancellationToken cancelToken);
    }
}
