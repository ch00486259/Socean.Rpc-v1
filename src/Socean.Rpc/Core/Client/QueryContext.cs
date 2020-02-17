using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    internal class SyncQueryContext : IQueryContext
    {
        public SyncQueryContext()
        {

        }

        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        //private readonly ResetEvent _autoResetEvent = new ResetEvent();

        private volatile FrameData _frameData;
        private volatile int _messageId = -1;

        public void Reset(int messageId)
        {
            _messageId = messageId;
            _frameData = null;
        }

        public bool OnReceive(FrameData frameData)
        {
            if (frameData == null)
                return false;

            if (_messageId != frameData.MessageId)
                return false;
            
            _frameData = frameData;
            _autoResetEvent.Set();

            return true;
        }

        public Task<FrameData> WaitForResult(int messageId, int millisecondsTimeout)
        {
            if (millisecondsTimeout == 0)
            {
                var _receiveData = _frameData;
                _frameData = null;
                _messageId = -1;

                return Task.FromResult(_receiveData);
            }
            
            _autoResetEvent.WaitOne(millisecondsTimeout);
             
            var receiveData = _frameData;
            _frameData = null;
            _messageId = -1;

            return Task.FromResult(receiveData);
        }        
    }

    internal class AsyncQueryContext : IQueryContext
    {
        public AsyncQueryContext()
        {

        }

        private volatile FrameData _frameData;
        private volatile int _messageId = -1;

        public void Reset(int messageId)
        {
            _messageId = messageId;
            _frameData = null;
        }

        public bool OnReceive(FrameData frameData)
        {
            if (frameData == null)
                return false;

            if (_messageId != frameData.MessageId)
                return false;

            _frameData = frameData;
            return true;
        } 

        public async Task<FrameData> WaitForResult(int messageId, int millisecondsTimeout)
        {
            if (millisecondsTimeout == 0)
            {
                var _receiveData = _frameData;
                _frameData = null;
                _messageId = -1;

                return _receiveData;
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            while (true)
            {
                if (_frameData != null)
                    break;

                if (stopWatch.ElapsedMilliseconds > millisecondsTimeout)
                    break;

                await Task.Delay(NetworkSettings.ClientDetectReceiveInterval);
            }

            stopWatch.Stop();

            var receiveData = _frameData;
            _frameData = null;
            _messageId = -1;

            return receiveData;
        }
    }

    internal interface IQueryContext
    {
        void Reset(int messageId);

        bool OnReceive(FrameData frameData);

        Task<FrameData> WaitForResult(int messageId, int millisecondsTimeout);
    }
}
