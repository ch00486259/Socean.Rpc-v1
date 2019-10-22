using System;
using System.Collections.Concurrent;
using System.Net;

namespace Socean.Rpc.Core.Client
{
    public sealed class SimpleRpcClientFactory
    {
        private static readonly ConcurrentDictionary<string, SimpleRpcClientFactory> _factoryDictionary = new ConcurrentDictionary<string, SimpleRpcClientFactory>();

        public static IClient Create(IPAddress ip, int port)
        {
            var factory = GetOrAddFactory(ip, port);
            return factory.Create();
        }

        public static void TakeBack(IClient client)
        {
            var rpcClient = client as SimpleRpcClient;
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

            var factory = GetOrAddFactory(rpcClient.ServerIP, rpcClient.ServerPort);
            factory.TakeBackInternal(rpcClient);
        }

        private static SimpleRpcClientFactory GetOrAddFactory(IPAddress ip, int port)
        {
            string key = ip + "_" + port;

            SimpleRpcClientFactory factory = null;

            _factoryDictionary.TryGetValue(key, out factory);
            if (factory != null)
                return factory;

            _factoryDictionary.TryAdd(key, new SimpleRpcClientFactory(ip, port));

            _factoryDictionary.TryGetValue(key, out factory);
            if (factory != null)
                return factory;

            throw new Exception("GetOrAddFactory error");
        }



        private SimpleRpcClientFactory(IPAddress ip, int port)
        {
            this._ip = ip;
            this._port = port;
        }

        private readonly ConcurrentQueue<IClient> _clientQueue = new ConcurrentQueue<IClient>();
        private readonly IPAddress _ip;
        private readonly int _port;


        private IClient Create()
        {
            _clientQueue.TryDequeue(out var rpcClient);
            if (rpcClient != null)
                return rpcClient;

            return new SimpleRpcClient(_ip, _port);
        }

        private void TakeBackInternal(SimpleRpcClient rpcClient)
        {
            if (_clientQueue.Count >= NetworkSettings.ClientCacheSize)
            {
                try
                {
                    rpcClient.Close();
                }
                catch
                {

                }
                return;
            }

            _clientQueue.Enqueue(rpcClient);
        }
    }
}
