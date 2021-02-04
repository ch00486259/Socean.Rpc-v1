using System;

namespace Socean.Rpc.Core
{
    internal class BytesCache
    {
        internal BytesCache(int defaultByteLength)
        {
            if (defaultByteLength <= 0)
                throw new RpcException("BytesCache length error");

            _defaultByteLength = defaultByteLength;
            _buffer = new byte[_defaultByteLength];
        }

        private readonly int _defaultByteLength;

        private volatile byte[] _buffer;

        public byte[] Get(int byteCount)
        {
            if (byteCount > _defaultByteLength)
                return new byte[byteCount];

            //var original = Interlocked.Exchange(ref _buffer, null);

            var original = _buffer;
            _buffer = null;

            if (original != null)
                return original;

            return new byte[_defaultByteLength];
        }

        public void Cache(byte[] bytes)
        {
            if (bytes == null)
                return;

            if (bytes.Length != _defaultByteLength)
                return;

            _buffer = bytes;
        }
    }
}
