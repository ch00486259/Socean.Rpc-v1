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
        private readonly IClientFactory _clientFactory;

        internal ShortConnectionRpcClient(IPAddress ip, int port)
        {
            ServerIP = ip;
            ServerPort = port;

            _clientFactory = AutoReconnectRpcClientFactory.GetOrAddFactory(ip, port);
            _client = _clientFactory.Create();
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

            _clientFactory.TakeBack(oldValue);
        }

        public void Dispose()
        {
            Close();
        }
    }

   
}
