using Socean.Rpc.Core.Message;
using System.Threading.Tasks;

namespace Socean.Rpc.Core.Client
{
    internal class HighResponseAsyncQueryContext : IAsyncQueryContext
    {
        internal HighResponseAsyncQueryContext()
        {

        }

        private volatile FrameData _frameData;
        private readonly QueryContextTaskCompletionSource _taskCompletionSource = new QueryContextTaskCompletionSource();

        public void Reset()
        {
            _frameData = null;
            _taskCompletionSource.Reset();
        }

        public bool OnReceiveResult(FrameData frameData)
        {
            if (frameData == null)
                return false;

            _frameData = frameData;
            _taskCompletionSource.SetSignal();

            return true;
        }

        public async Task WaitForResult(int millisecondsTimeout, AsyncFrameDataFacade asyncFrameDataFacade)
        {
            if (millisecondsTimeout <= 0)
            {
                asyncFrameDataFacade.FrameData = _frameData;
                _frameData = null;

                return;
            }

            _taskCompletionSource.SetSignalTimeout(millisecondsTimeout);
            await _taskCompletionSource.Task;

            var receiveData = _frameData;
            _frameData = null;

            asyncFrameDataFacade.FrameData = receiveData;
            return;
        }
    }
}
