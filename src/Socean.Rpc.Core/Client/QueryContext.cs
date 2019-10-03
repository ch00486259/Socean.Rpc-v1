using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    public class SyncQueryContext : IQueryContext
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

    public interface IQueryContext
    {
        void Reset(int messageId);

        bool OnReceive(FrameData frameData);

        Task<FrameData> WaitForResult(int messageId, int millisecondsTimeout);
    }

    internal class HighResponseQueryContextFacade : IQueryContext
    {
        public HighResponseQueryContextFacade(IQueryContext queryContext)
        {
            _queryContext = queryContext;
        }

        private IQueryContext _queryContext;

        private TaskCompletionSource<int> _taskCompletionSource;

        public void Reset(int messageId)
        {
            _queryContext.Reset(messageId);
                     
            _taskCompletionSource = new TaskCompletionSource<int>();
        }

        public bool OnReceive(FrameData frameData)
        {
            var isReceiveValid = _queryContext.OnReceive(frameData);
            if (!isReceiveValid)
                return false;

            if (_taskCompletionSource != null)
                _taskCompletionSource.SetResult(0);

            return true;
        }
             
        public async Task<FrameData> WaitForResult(int messageId, int millisecondsTimeout)
        {
            var loopCount = (millisecondsTimeout / 15) + 1;

            for (var i = 0; i < loopCount; i++)
            {
                var task = Task.Delay(15);

                var t = Task.WhenAny(task, _taskCompletionSource.Task);
                await t;

                if (_taskCompletionSource.Task.IsCompleted)
                    break;
            }

            _taskCompletionSource = null;

            return await _queryContext.WaitForResult(messageId, 0);
        }
    }

    public class ResetEvent
    {
        private object _key = new object();

        private bool _setFlag = false;

        public void Set()
        {
            lock (_key)
            {
                _setFlag = true;

                Monitor.Pulse(_key);
            }
        }

        public void WaitOne(int timeoutMilliseconds)
        {
            lock (_key)
            {
                if (_setFlag == true)
                {
                    _setFlag = false;
                    return;
                }

                var isTimeout = Monitor.Wait(_key, timeoutMilliseconds);
                if (isTimeout == false)
                {
                    _setFlag = false;
                    return;
                }
                _setFlag = false;
            }
        }
    }
}
