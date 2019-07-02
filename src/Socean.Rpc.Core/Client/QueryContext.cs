using System;
using System.Threading;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    internal class QueryContext
    {
        private FrameData _receiveData;
        private int _messageId;

        public void Reset(int messageId)
        {
            _messageId = messageId;
            _receiveData = null;
        }

        public void OnReceive(FrameData frameData)
        {
            _receiveData = frameData;
        }

        public FrameData WaitForResult()
        {
            var loopCount = 0;
            if (NetworkSettings.ReceiveTimeout > 0 && NetworkSettings.ClientDetectReceiveInterval > 0)
                loopCount = NetworkSettings.ReceiveTimeout / NetworkSettings.ClientDetectReceiveInterval;

            if (loopCount == 0)
                loopCount = 1;

            for (int i = 0; i < loopCount; i++)
            {
                Thread.Sleep(NetworkSettings.ClientDetectReceiveInterval);

                if (_receiveData != null)
                    return _receiveData;
            }

            return null;
        }

        public async Task<FrameData> WaitForResultAsync()
        {
            var loopCount = 0;
            if (NetworkSettings.ReceiveTimeout > 0 && NetworkSettings.ClientDetectReceiveInterval > 0)
                loopCount = NetworkSettings.ReceiveTimeout / NetworkSettings.ClientDetectReceiveInterval;

            if (loopCount == 0)
                loopCount = 1;

            for (var i = 0; i < loopCount; i++)
            {
                await Task.Delay(NetworkSettings.ClientDetectReceiveInterval);

                if (_receiveData != null)
                    return _receiveData;
            }

            return null;
        }
    }
}
