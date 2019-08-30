using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    public sealed class FastRpcClient: IClient
    {
        public IPAddress ServerIP { get; }
        public int ServerPort { get; }

        private SimpleRpcClient _client;

        public FastRpcClient(IPAddress ip, int port)
        {
            ServerIP = ip;
            ServerPort = port;

            _client = (SimpleRpcClient)SimpleRpcClientFactory.Create(ip, port);
        }

        public FrameData Query(string title, byte[] contentBytes, bool throwIfErrorResponseCode = false)
        {
            if (_client == null)
                throw new Exception("client has been closed");

            return _client.Query(title, contentBytes, throwIfErrorResponseCode);
        }

        internal async Task<FrameData> QueryAsync(string title, byte[] contentBytes, bool throwIfErrorResponseCode = false)
        {
            if (_client == null)
                throw new Exception("client has been closed");

            return await _client.QueryAsync(title, contentBytes, throwIfErrorResponseCode);
        }

        public void Close()
        {
            if (_client == null)
                return;

            var oldValue = Interlocked.Exchange(ref _client, null);
            if (oldValue == null)
                return;

            SimpleRpcClientFactory.TakeBack(oldValue);
        }

        public void Dispose()
        {
            Close();
        }
    }

   
}
