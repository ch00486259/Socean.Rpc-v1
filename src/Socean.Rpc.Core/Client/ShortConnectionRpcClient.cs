using System;
using System.Net;
using System.Threading;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    public sealed class ShortConnectionRpcClient: IClient
    {
        public IPAddress ServerIP { get; }
        public int ServerPort { get; }

        private IClient _client;

        internal ShortConnectionRpcClient(IPAddress ip, int port)
        {
            ServerIP = ip;
            ServerPort = port;

            _client = AutoReconnectRpcClientFactory.Create(ip, port);
        }

        public FrameData Query(string title, byte[] contentBytes, bool throwIfErrorResponseCode = false)
        {
            if (_client == null)
                throw new Exception("client has been closed");

            return _client.Query(title, contentBytes, throwIfErrorResponseCode);
        }

        public void Close()
        {
            if (_client == null)
                return;

            var oldValue = Interlocked.Exchange(ref _client, null);
            if (oldValue == null)
                return;

            AutoReconnectRpcClientFactory.TakeBack(oldValue);
        }

        public void Dispose()
        {
            Close();
        }
    }

   
}
