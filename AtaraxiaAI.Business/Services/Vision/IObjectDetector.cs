using System;
using System.Threading;

namespace AtaraxiaAI.Business.Services
{
    internal interface IObjectDetector
    {
        void Initiate(Action<byte[]> updateFrameAction, CancellationToken cancelToken);
    }
}
