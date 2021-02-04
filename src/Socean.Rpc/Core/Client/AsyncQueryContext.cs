using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    internal class AsyncQueryContext : IAsyncQueryContext
    {
        internal AsyncQueryContext()
        {

        }

        private volatile FrameData _frameData;
        private volatile TaskCompletionSource<int> _taskCompletionSource;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private const int DEFAULT_DELAY_MS = 10;

        public void Reset()
        {
            _frameData = null;
            _taskCompletionSource = new TaskCompletionSource<int>();
        }

        public bool OnReceiveResult(FrameData frameData)
        {
            if (frameData == null)
                return false;

            _frameData = frameData;
            _taskCompletionSource?.SetResult(0);

            return true;
        }

        public async Task WaitForResult(int millisecondsTimeout,AsyncFrameDataFacade asyncFrameDataFacade)
        {
            if (millisecondsTimeout <= 0)
            {
                asyncFrameDataFacade.FrameData = _frameData;
                _frameData = null;

                return;
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

            asyncFrameDataFacade.FrameData = receiveData;
            return;
        }

        public void Dispose()
        {             
            
        }
    }

    public interface IAsyncQueryContext:IDisposable
    {
        void Reset();

        bool OnReceiveResult(FrameData frameData);

        Task WaitForResult(int millisecondsTimeout, AsyncFrameDataFacade asyncFrameDataFacade);
    }   
}
