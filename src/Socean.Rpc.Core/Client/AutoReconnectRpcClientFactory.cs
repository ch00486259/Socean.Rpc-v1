using System;
using System.Collections.Concurrent;
using System.Net;

namespace Socean.Rpc.Core.Client
{
    public sealed class AutoReconnectRpcClientFactory: IClientFactory
    {
        private static readonly ConcurrentDictionary<string, AutoReconnectRpcClientFactory> _factoryDictionary = new ConcurrentDictionary<string, AutoReconnectRpcClientFactory>();

        public static AutoReconnectRpcClientFactory GetOrAddFactory(IPAddress ip, int port)
        {
            string key = ip + "_" + port;

            AutoReconnectRpcClientFactory factory = null;

            _factoryDictionary.TryGetValue(key, out factory);
            if (factory != null)
                return factory;

            _factoryDictionary.TryAdd(key, new AutoReconnectRpcClientFactory(ip, port));

            _factoryDictionary.TryGetValue(key, out factory);
            if (factory != null)
                return factory;

            throw new Exception();
        }



        private AutoReconnectRpcClientFactory(IPAddress ip, int port)
        {
            this._ip = ip;
            this._port = port;
        }

        private readonly ConcurrentQueue<IClient> _clientQueue = new ConcurrentQueue<IClient>();
        private readonly IPAddress _ip;
        private readonly int _port;


        public IClient Create()
        {
            _clientQueue.TryDequeue(out var rpcClient);
            if (rpcClient != null)
                return rpcClient;

            return new AutoReconnectRpcClient(_ip, _port);
        }

        public void TakeBack(IClient client)
        {
            var rpcClient = client as AutoReconnectRpcClient;
            if (rpcClient == null)
            {
                try
                {
                    client.Close();
                }
                catch
                {

                }
                return;
            }

            if (_clientQueue.Count >= NetworkSettings.ClientCacheSize)
            {
                try
                {
                    client.Close();
                }
                catch
                {

                }
                return;
            }

            _clientQueue.Enqueue(client);
        }
    }
}
