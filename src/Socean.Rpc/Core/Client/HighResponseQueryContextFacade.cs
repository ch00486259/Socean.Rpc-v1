using System;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
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
}
