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

        private SimpleRpcClient _client;

        public FastRpcClient(IPAddress ip, int port)
        {
            ServerIP = ip;
            ServerPort = port;

            _client = (SimpleRpcClient)SimpleRpcClientPoolRoot.GetItem(ip, port);
        }

        public FrameData Query(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = true)
        {
            if (_client == null)
                throw new RpcException("client has been closed");

            return _client.Query(titleBytes, contentBytes, extentionBytes, throwIfErrorResponseCode);
        }

        public async Task<FrameData> QueryAsync(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = true)
        {
            if (_client == null)
                throw new RpcException("client has been closed");

            return await _client.QueryAsync(titleBytes, contentBytes, extentionBytes, throwIfErrorResponseCode);
        }

        public void Close()
        {
            if (_client == null)
                return;

            var oldValue = Interlocked.Exchange(ref _client, null);
            if (oldValue == null)
                return;

            var cacheResult = SimpleRpcClientPoolRoot.ReturnItem(oldValue);
            if (cacheResult == false)
            {
                try
                {
                    oldValue.Dispose();
                }
                catch
                {
                    LogAgent.Error("SimpleRpcCLient Dispose error");
                }
            }
        }

        public void Dispose()
        {
            Close();
        }
    }   
}
