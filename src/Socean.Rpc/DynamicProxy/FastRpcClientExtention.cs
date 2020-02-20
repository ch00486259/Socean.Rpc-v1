using Socean.Rpc.Core;
using Socean.Rpc.Core.Client;
using Socean.Rpc.Core.Message;
using System;

namespace Socean.Rpc.DynamicProxy
{
    internal static class FastRpcClientExtention
    {
        internal static object QueryInternal(this FastRpcClient fastRpcClient, string title, ICustomTuple customTuple, Type returnType, IRpcSerializer rpcSerializer, string extention = null, bool throwIfErrorResponseCode = false)
        {
            if (rpcSerializer == null)
                throw new ArgumentNullException(nameof(rpcSerializer));

            if (customTuple == null)
                throw new ArgumentNullException(nameof(customTuple));

            var encoding = RpcExtentionSettings.DefaultEncoding;

            var response = fastRpcClient.Query(
                encoding.GetBytes(title),
                encoding.GetBytes(rpcSerializer.Serialize(customTuple)),
                extention == null ? FrameFormat.EmptyBytes : encoding.GetBytes(extention),
                throwIfErrorResponseCode);

            if (returnType == typeof(void))
                return null;

            var responseContent = response.ReadContentString();
            if (string.IsNullOrEmpty(responseContent))
                return null;

            return rpcSerializer.Deserialize(responseContent, returnType);
        }
    }
}
