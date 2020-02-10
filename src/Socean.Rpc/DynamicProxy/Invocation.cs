using System;
using System.Reflection;

namespace Socean.Rpc.DynamicProxy
{
    internal class Invocation
    {
        public readonly string Key;
        public readonly object HandlerTarget;
        public readonly Type HandlerParameterTupleType;
        public readonly MethodInfo HandlerMethodInfo;
        public readonly IRpcSerializer RpcSerializer;
        private readonly FastInvokeHandler _fastInvokeHandler;

        public Invocation(string key, object target, MethodInfo methodInfo, Type handlerParametersTupleType, IRpcSerializer rpcSerializer)
        {
            Key = key;
            HandlerTarget = target;
            HandlerMethodInfo = methodInfo;
            HandlerParameterTupleType = handlerParametersTupleType;
            RpcSerializer = rpcSerializer;
            _fastInvokeHandler = DynamicProxyHelper.CreateFastInvokeHandler(HandlerMethodInfo);
        }

        public string Proceed(string content)
        {
            var parameterTuple = (ICustomTuple)RpcSerializer.Deserialize(content, HandlerParameterTupleType);
            var parameterArray = parameterTuple.ToObjectArray();
            var result = _fastInvokeHandler.Invoke(HandlerTarget, parameterArray);
            if (result == null)
                return string.Empty;
            return RpcSerializer.Serialize(result);
        }
    }
}
