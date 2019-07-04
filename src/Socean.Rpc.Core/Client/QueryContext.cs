using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    internal class QueryContext
    {
        private FrameData _receiveData;
        private volatile int _messageId;
        private volatile bool _isCompleted = true;

        public void Reset(int messageId)
        {
            _messageId = messageId;
            _receiveData = null;
            _isCompleted = false;
        }

        public void OnReceive(FrameData frameData)
        {
            _receiveData = frameData;
        }

        public bool IsCompleted
        {
            get => _isCompleted;
        }

        public FrameData WaitForResult()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            
            while (true)
            {
                if(stopWatch.ElapsedMilliseconds > NetworkSettings.ReceiveTimeout)
                    break;

                Thread.Sleep(NetworkSettings.ClientDetectReceiveInterval);

                if (_receiveData != null)
                {
                    _isCompleted = true;
                    stopWatch.Stop();
                    return _receiveData;
                }
            }

            _isCompleted = true;
            stopWatch.Stop();
            return null;
        }

        public async Task<FrameData> WaitForResultAsync()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            while (true)
            {
                if (stopWatch.ElapsedMilliseconds > NetworkSettings.ReceiveTimeout)
                    break;

                await Task.Delay(NetworkSettings.ClientDetectReceiveInterval);

                if (_receiveData != null)
                {
                    _isCompleted = true;
                    stopWatch.Stop();
                    return _receiveData;
                }
            }

            _isCompleted = true;
            stopWatch.Stop();
            return null;
        }
    }
}
