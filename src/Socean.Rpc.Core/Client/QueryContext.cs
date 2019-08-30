using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    internal class QueryContext
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
}
