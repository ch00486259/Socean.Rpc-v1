using Socean.Rpc.Core.Message;
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
        public readonly IBinarySerializer BinarySerializer;
        private readonly FastInvokeHandler _fastInvokeHandler;

        public Invocation(string key, Type handlerTargetType, MethodInfo handlerMethodInfo, Type handlerParametersTupleType, IBinarySerializer serializer)
        {
            Key = key;
            HandlerTargetType = handlerTargetType;
            HandlerMethodInfo = handlerMethodInfo;
            HandlerParameterTupleType = handlerParametersTupleType;
            BinarySerializer = serializer;
            _fastInvokeHandler = DynamicProxyHelper.CreateFastInvokeHandler(HandlerMethodInfo);
        }

        public byte[] Proceed(byte[] contentBytes)
        {
            var handlerTarget = ObjectFactory.CreateInstance(HandlerTargetType);
            var parameterTuple = (ICustomTuple)BinarySerializer.Deserialize(contentBytes, HandlerParameterTupleType);
            var parameterArray = parameterTuple.ToObjectArray();
            var result = _fastInvokeHandler.Invoke(handlerTarget, parameterArray);
            if (result == null)
                return FrameFormat.EmptyBytes;
            return BinarySerializer.Serialize(result);
        }
    }
}
