using System;
using System.Threading;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    internal class SyncQueryContext 
    {
        internal SyncQueryContext()
        {

        }

        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        private volatile FrameData _frameData;
        private volatile int _waitingMessageId = -1;
        private volatile bool _receivedOneSet = false;

        public void Reset(int messageId)
        {
            _waitingMessageId = messageId;
            _frameData = null;

            if (_receivedOneSet == false)
                _autoResetEvent.Reset();
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

        public FrameData WaitForResult(int messageId, int millisecondsTimeout)
        {
            if (millisecondsTimeout <= 0)
            {
                var _receiveData = _frameData;
                _frameData = null;
                _waitingMessageId = -1;

                return _receiveData;
            }

            _receivedOneSet = _autoResetEvent.WaitOne(millisecondsTimeout);
             
            var receiveData = _frameData;
            _frameData = null;
            _waitingMessageId = -1;

            return receiveData;
        }        
    }
}
