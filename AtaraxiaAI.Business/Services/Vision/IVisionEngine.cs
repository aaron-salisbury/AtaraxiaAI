using System;
using System.Threading;

namespace AtaraxiaAI.Business.Services
{
    internal interface IVisionEngine
    {
        void Initiate(Action<byte[]> updateFrameAction, CancellationToken cancelToken);
    }
}
