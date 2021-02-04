using System;
using System.Collections.Concurrent;
using System.Net;

namespace Socean.Rpc.Core.Client
{
    public class SimpleRpcClientPolicy : IObjectPoolPolicy<SimpleRpcClient>
    {
        private readonly IPAddress _ip;
        private readonly int _port;

        public SimpleRpcClientPolicy(IPAddress ip, int port)
        {
            if (ip == null)
                throw new ArgumentNullException("ip");

            _ip = ip;
            _port = port;
        }

        public SimpleRpcClient Create()
        {
            return new SimpleRpcClient(_ip, _port);
        }

        public bool CanReturn(SimpleRpcClient obj)
        {
            return true;
        }
    }

    public interface IObjectPoolPolicy<T>
    {
        T Create();

        bool CanReturn(T obj);
    }

    public class SimpleRpcClientPool : ObjectPool<SimpleRpcClient>
    {
        public SimpleRpcClientPool(IObjectPoolPolicy<SimpleRpcClient> objectPoolPolicy) : base(objectPoolPolicy)
        { 
        
        }
    }

    public abstract class ObjectPool<T> where T : class
    {
        private readonly ConcurrentBag<T> _clientList = new ConcurrentBag<T>();
        private readonly IObjectPoolPolicy<T> _objectPoolPolicy;

        public ObjectPool(IObjectPoolPolicy<T> objectPoolPolicy)
        {
            if (objectPoolPolicy == null)
                throw new ArgumentNullException("objectPoolPolicy");

            _objectPoolPolicy = objectPoolPolicy;
        }

        public T Get()
        {
            _clientList.TryTake(out var _obj);
            if (_obj != null)
                return _obj;

            return _objectPoolPolicy.Create();
        }

        public bool TakeBack(T obj)
        {
            if (_objectPoolPolicy.CanReturn(obj) && _clientList.Count < NetworkSettings.ClientCacheSize)
            {
                _clientList.Add(obj);
                return true;
            }                      

            return false;
        }
    }

    public sealed class SimpleRpcClientPoolRoot
    {
        private static readonly ConcurrentDictionary<string, SimpleRpcClientPool> _poolDictionary = new ConcurrentDictionary<string, SimpleRpcClientPool>();

        public static SimpleRpcClient Depool(IPAddress ip, int port)
        {
            var factory = GetOrAddPool(ip, port);
            return factory.Get();
        }

        public static void Enpool(SimpleRpcClient rpcClient)
        {
            var factory = GetOrAddPool(rpcClient.ServerIP, rpcClient.ServerPort);
            var cacheResult = factory.TakeBack(rpcClient);
            if (cacheResult == false)
            {
                try
                {
                    rpcClient.Dispose();
                }
                catch
                {
                    LogAgent.Warn("SimpleRpcClient Dispose error");
                }
            }
        }

        private static SimpleRpcClientPool GetOrAddPool(IPAddress ip, int port)
        {
            string key = ip + "_" + port;

            SimpleRpcClientPool pool = null;

            _poolDictionary.TryGetValue(key, out pool);
            if (pool != null)
                return pool;

            _poolDictionary.TryAdd(key, new SimpleRpcClientPool(new SimpleRpcClientPolicy(ip, port)));

            _poolDictionary.TryGetValue(key, out pool);
            if (pool != null)
                return pool;

            throw new RpcException("SimpleRpcClientPoolProvider GetOrAddPool error");
        }
    }
}
