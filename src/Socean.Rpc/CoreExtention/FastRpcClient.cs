using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    public sealed partial class FastRpcClient: IClient
    {
        public IPAddress ServerIP { get; }
        public int ServerPort { get; }

        private volatile SimpleRpcClient _client;

        /// <summary>
        /// 0 idle, 1 querying
        /// </summary>
        private volatile int _queryState;

        public FastRpcClient(IPAddress ip, int port)
        {
            ServerIP = ip;
            ServerPort = port;

            _client = (SimpleRpcClient)SimpleRpcClientPoolRoot.Depool(ip, port);
        }

        private void TransportKeepAlive()
        {
            if (_client.IsSocketConnected == false)
            {
                try
                {
                    _client.Close();
                }
                catch
                {

                }
            }

            if (_client.TransportState == TcpTransportState.Closed)
            {
                var oldClient = _client;
                if (oldClient == null)
                    throw new RpcException("FastRpcClient TransportKeepAlive failed,client has been been closed");

                var newClient = (SimpleRpcClient)SimpleRpcClientPoolRoot.Depool(ServerIP, ServerPort);
                var originalClient = Interlocked.CompareExchange(ref _client, newClient, oldClient);
                if (originalClient != oldClient)
                {
                    SimpleRpcClientPoolRoot.Enpool(newClient);
                    throw new RpcException("FastRpcClient TransportKeepAlive failed,client has been been closed");
                }
            }
        }
        
        public FrameData Query(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = true)
        {
            if (_client == null)
                throw new RpcException("FastRpcClient Query failed,client has been been closed");

            var originalQueryState = Interlocked.CompareExchange(ref _queryState, 1, 0);
            if (originalQueryState != 0)
                throw new RpcException("FastRpcClient Query failed,client is querying");

            try
            {
                TransportKeepAlive();
                return _client.Query(titleBytes, contentBytes, extentionBytes, throwIfErrorResponseCode);
            }
            finally
            {
                _queryState = 0;
            }
        }

        public async Task<FrameData> QueryAsync(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = true)
        {
            if (_client == null)
                throw new RpcException("FastRpcClient QueryAsync failed,client has been closed");

            var originalQueryState = Interlocked.CompareExchange(ref _queryState, 1, 0);
            if (originalQueryState != 0)
                throw new RpcException("FastRpcClient QueryAsync failed,client is querying");

            try
            {
                TransportKeepAlive();
                return await _client.QueryAsync(titleBytes, contentBytes, extentionBytes, throwIfErrorResponseCode).ConfigureAwait(false);
            }
            finally
            {
                _queryState = 0;
            }
        }

        public void Close()
        {
            var originalClient = Interlocked.Exchange(ref _client, null);
            if (originalClient == null)
                return;

            if (originalClient.TransportState == TcpTransportState.Closed)
                return;

            var cacheResult = SimpleRpcClientPoolRoot.Enpool(originalClient);
            if (cacheResult == false)
            {
                try
                {
                    originalClient.Dispose();
                }
                catch
                {
                    LogAgent.Warn("SimpleRpcClient Dispose error");
                }
            }
        }

        public void Dispose()
        {
            Close();
        }
    }   
}
