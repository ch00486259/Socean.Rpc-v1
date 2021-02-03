using System;
using System.Threading;
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
        private volatile bool _receivedOneSet = false;

        internal void Reset()
        {
            _frameData = null;

            if (_receivedOneSet == false)
                _autoResetEvent.Reset();
        }

        internal bool OnReceiveResult(FrameData frameData)
        {
            if (frameData == null)
                return false;
            
            _frameData = frameData;
            _autoResetEvent.Set();

            return true;
        }

        internal FrameData WaitForResult(int millisecondsTimeout)
        {
            if (millisecondsTimeout <= 0)
            {
                var _receiveData = _frameData;
                _frameData = null;

                return _receiveData;
            }

            _receivedOneSet = _autoResetEvent.WaitOne(millisecondsTimeout);
             
            var receiveData = _frameData;
            _frameData = null;

            return receiveData;
        }        
    }
}
