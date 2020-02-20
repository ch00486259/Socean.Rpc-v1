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

        //private readonly ResetEvent _autoResetEvent = new ResetEvent();
        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
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

        public FrameData WaitForResult(int messageId, int millisecondsTimeout)
        {
            if (millisecondsTimeout == 0)
            {
                var _receiveData = _frameData;
                _frameData = null;
                _messageId = -1;

                return _receiveData;
            }
            
            _autoResetEvent.WaitOne(millisecondsTimeout);
             
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

        FrameData WaitForResult(int messageId, int millisecondsTimeout);
    }
}
