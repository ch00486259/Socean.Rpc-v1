using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    internal class SyncQueryContext : IQueryContext
    {
        internal SyncQueryContext()
        {

        }

        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        private volatile FrameData _frameData;
        private volatile int _waitingMessageId = -1;

        public void Reset(int messageId)
        {
            _waitingMessageId = messageId;
            _frameData = null;
        }

        public bool OnReceiveResult(FrameData frameData)
        {
            if (frameData == null)
                return false;

            if (_waitingMessageId != frameData.MessageId)
                return false;
            
            _frameData = frameData;
            _autoResetEvent.Set();

            return true;
        }

        public Task<FrameData> WaitForResult(int messageId, int millisecondsTimeout)
        {
            if (millisecondsTimeout <= 0)
            {
                var _receiveData = _frameData;
                _frameData = null;
                _waitingMessageId = -1;

                return Task.FromResult(_receiveData);
            }
            
            _autoResetEvent.WaitOne(millisecondsTimeout);
             
            var receiveData = _frameData;
            _frameData = null;
            _waitingMessageId = -1;

            return Task.FromResult(receiveData);
        }        
    }

    internal interface IQueryContext
    {
        void Reset(int messageId);

        bool OnReceiveResult(FrameData frameData);

        Task<FrameData> WaitForResult(int messageId, int millisecondsTimeout);
    }
}
