using System;
using System.Net;
using Socean.Rpc.Core.Client;

namespace Socean.Rpc.DynamicProxy
{
    public class EasyProxyInterceptor : IInterceptor
    {
        public object Proceed(ProxyBase proxyTarget, string title, Type parameterTupleType, object[] parameterArray, Type returnType)
        {
            var customTuple = (ICustomTuple)ObjectFactory.CreateInstance(parameterTupleType);
            customTuple.Fill(parameterArray);

            using (var fastRpcClient = new FastRpcClient(proxyTarget.__IP, proxyTarget.__Port))
            {
                return fastRpcClient.QueryInternal(title, customTuple, returnType, proxyTarget.__RpcSerializer, throwIfErrorResponseCode: true,extention: proxyTarget.__Extention);
            }
        }
    }   
}
