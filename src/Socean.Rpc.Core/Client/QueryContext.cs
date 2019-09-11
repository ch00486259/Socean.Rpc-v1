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
        internal readonly ConcurrentDictionary<int, FrameData> _receiveDataDictionary = new ConcurrentDictionary<int, FrameData>();

        public void Reset(int messageId)
        {
            _receiveDataDictionary[messageId] = null;
        }

        public void OnReceive(FrameData frameData)
        {
            if(frameData != null)
                _receiveDataDictionary.TryUpdate(frameData.MessageId, frameData, null);
        }

        public FrameData WaitForResult(int messageId)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            FrameData receiveData = null;

            while (true)
            {
                if(stopWatch.ElapsedMilliseconds > NetworkSettings.ReceiveTimeout)
                    break;

                Thread.Sleep(NetworkSettings.ClientDetectReceiveInterval);
              
                _receiveDataDictionary.TryGetValue(messageId, out receiveData);
                if (receiveData != null)
                    break;
            }

            _receiveDataDictionary.TryRemove(messageId, out var _);
            stopWatch.Stop();
            return receiveData;
        }

        public async Task<FrameData> WaitForResultAsync(int messageId)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            FrameData receiveData = null;

            while (true)
            {
                if (stopWatch.ElapsedMilliseconds > NetworkSettings.ReceiveTimeout)
                    break;

                await Task.Delay(NetworkSettings.ClientDetectReceiveInterval);

                _receiveDataDictionary.TryGetValue(messageId, out receiveData);
                if (receiveData != null)
                    break;
            }

            _receiveDataDictionary.TryRemove(messageId, out var _);
            stopWatch.Stop();
            return receiveData;
        }
    }

    public interface IQueryContext
    {
        void Reset(int messageId);

        void OnReceive(FrameData frameData);

        FrameData WaitForResult(int messageId);

        Task<FrameData> WaitForResultAsync(int messageId);
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

        public FrameData WaitForResult(int messageId)
        {
            _autoResetEvent.WaitOne(NetworkSettings.ReceiveTimeout);

            _queryContext._receiveDataDictionary.TryRemove(messageId, out var receiveData);
            return receiveData;
        }

        public async Task<FrameData> WaitForResultAsync(int messageId)
        {
            return await _queryContext.WaitForResultAsync(messageId);
        }
    }
}
