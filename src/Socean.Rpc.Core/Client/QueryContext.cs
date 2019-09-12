using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    internal class QueryContext: IQueryContext
    {
        private readonly ConcurrentDictionary<int, FrameData> _receiveDataDictionary = new ConcurrentDictionary<int, FrameData>();

        public void Reset(int messageId)
        {
            _receiveDataDictionary[messageId] = null;
        }

        public void OnReceive(FrameData frameData)
        {
            if(frameData != null)
                _receiveDataDictionary.TryUpdate(frameData.MessageId, frameData, null);
        }

        public FrameData WaitForResult(int messageId, int millisecondsTimeout)
        {
            if (millisecondsTimeout > 0)
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                while (true)
                {
                    if (stopWatch.ElapsedMilliseconds > millisecondsTimeout)
                        break;

                    Thread.Sleep(NetworkSettings.ClientDetectReceiveInterval);

                    _receiveDataDictionary.TryGetValue(messageId, out var _receiveData);
                    if (_receiveData != null)
                        break;
                }

                stopWatch.Stop();
            }

            _receiveDataDictionary.TryRemove(messageId, out var receiveData);
            return receiveData;
        }

        public async Task<FrameData> WaitForResultAsync(int messageId, int millisecondsTimeout)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            while (true)
            {
                if (stopWatch.ElapsedMilliseconds > millisecondsTimeout)
                    break;

                await Task.Delay(NetworkSettings.ClientDetectReceiveInterval);

                _receiveDataDictionary.TryGetValue(messageId, out var _receiveData);
                if (_receiveData != null)
                    break;
            }

            stopWatch.Stop();

            _receiveDataDictionary.TryRemove(messageId, out var receiveData);
            return receiveData;
        }
    }

    public interface IQueryContext
    {
        void Reset(int messageId);

        void OnReceive(FrameData frameData);

        FrameData WaitForResult(int messageId, int millisecondsTimeout);

        Task<FrameData> WaitForResultAsync(int messageId, int millisecondsTimeout);
    }

    internal class HighResponseQueryContextFacade : IQueryContext
    {
        public HighResponseQueryContextFacade(IQueryContext queryContext)
        {
            _queryContext = (QueryContext)queryContext;
        }

        private QueryContext _queryContext;
        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        public void Reset(int messageId)
        {
            _queryContext.Reset(messageId);

            _autoResetEvent.Reset();
        }

        public void OnReceive(FrameData frameData)
        {
            _queryContext.OnReceive(frameData);

            _autoResetEvent.Set();
        }

        public FrameData WaitForResult(int messageId, int millisecondsTimeout)
        {
            _autoResetEvent.WaitOne(millisecondsTimeout);

            return _queryContext.WaitForResult(messageId, 0);
        }

        public async Task<FrameData> WaitForResultAsync(int messageId, int millisecondsTimeout)
        {
            return await _queryContext.WaitForResultAsync(messageId, millisecondsTimeout);
        }
    }
}
