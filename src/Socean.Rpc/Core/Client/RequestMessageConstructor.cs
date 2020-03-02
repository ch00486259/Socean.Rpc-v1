using System;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    internal class RequestMessageConstructor
    {
        public volatile int MessageId;
        public volatile int MessageByteCount;
        public volatile byte[] SendBuffer;
        public volatile bool ThrowIfErrorResponseCode;
        private readonly BytesCache _sendBufferCache;

        internal RequestMessageConstructor()
        {
            _sendBufferCache = new BytesCache(NetworkSettings.WriteBufferSize);
        }

        internal void ConstructCurrentMessage(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = true)
        {
            MessageId++;
            if (MessageId >= 100000000)
                MessageId = 1;

            MessageByteCount = FrameFormat.ComputeFrameByteCount(extentionBytes, titleBytes, contentBytes);
            SendBuffer = _sendBufferCache.Get(MessageByteCount);
            FrameFormat.FillFrame(SendBuffer, extentionBytes, titleBytes, contentBytes, 0, MessageId);

            ThrowIfErrorResponseCode = throwIfErrorResponseCode;
        }

        internal void ClearCurrentMessage()
        {
            _sendBufferCache.Cache(SendBuffer);
        }
    }
}
