using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    internal class AsyncQueryContext : IQueryContext
    {
        internal AsyncQueryContext()
        {

        }

        private volatile FrameData _frameData;
        private volatile int _waitingMessageId = -1;
        private volatile TaskCompletionSource<int> _taskCompletionSource;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private const int DEFAULT_DELAY_MS = 10;

        public void Reset(int messageId)
        {
            _waitingMessageId = messageId;
            _frameData = null;
            _taskCompletionSource = new TaskCompletionSource<int>();
        }

        public bool OnReceiveResult(FrameData frameData)
        {
            if (frameData == null)
                return false;

            if (_waitingMessageId != frameData.MessageId)
                return false;

            _frameData = frameData;
            _taskCompletionSource?.SetResult(0);

            return true;
        }

        public async Task<FrameData> WaitForResult(int messageId, int millisecondsTimeout)
        {
            if (millisecondsTimeout <= 0)
            {
                var _receiveData = _frameData;
                _frameData = null;
                _waitingMessageId = -1;

                return _receiveData;
            }

            _stopwatch.Restart();

            var delayMillisecond = NetworkSettings.HighResponse ? 1 : DEFAULT_DELAY_MS;

            while (true)
            {
                var t = Task.WhenAny(Task.Delay(delayMillisecond), _taskCompletionSource.Task);
                await t;

                if (_taskCompletionSource.Task.IsCompleted)
                    break;

                if (_stopwatch.ElapsedMilliseconds >= millisecondsTimeout)
                    break;
            }

            _stopwatch.Stop();

            var receiveData = _frameData;
            _frameData = null;
            _waitingMessageId = -1;

            return receiveData;
        }
    }
}
