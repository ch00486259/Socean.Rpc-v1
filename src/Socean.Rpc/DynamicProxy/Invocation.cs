using System;
using System.Reflection;

namespace Socean.Rpc.DynamicProxy
{
    internal class Invocation
    {
        public readonly string Key;
        public readonly Type HandlerTargetType;
        public readonly Type HandlerParameterTupleType;
        public readonly MethodInfo HandlerMethodInfo;
        public readonly IRpcSerializer RpcSerializer;
        private readonly FastInvokeHandler _fastInvokeHandler;

        public Invocation(string key, Type handlerTargetType, MethodInfo handlerMethodInfo, Type handlerParametersTupleType, IRpcSerializer rpcSerializer)
        {
            Key = key;
            HandlerTargetType = handlerTargetType;
            HandlerMethodInfo = handlerMethodInfo;
            HandlerParameterTupleType = handlerParametersTupleType;
            RpcSerializer = rpcSerializer;
            _fastInvokeHandler = DynamicProxyHelper.CreateFastInvokeHandler(HandlerMethodInfo);
        }

        public string Proceed(string content)
        {
            var handlerTarget = ObjectFactory.CreateInstance(HandlerTargetType);
            var parameterTuple = (ICustomTuple)RpcSerializer.Deserialize(content, HandlerParameterTupleType);
            var parameterArray = parameterTuple.ToObjectArray();
            var result = _fastInvokeHandler.Invoke(handlerTarget, parameterArray);
            if (result == null)
                return string.Empty;
            return RpcSerializer.Serialize(result);
        }
    }
}
