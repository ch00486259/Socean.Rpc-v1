using Socean.Rpc.Core;
using Socean.Rpc.Core.Client;
using Socean.Rpc.Core.Message;
using System;

namespace Socean.Rpc.DynamicProxy
{
    internal static class FastRpcClientExtention
    {
        internal static object QueryInternal(this FastRpcClient fastRpcClient, string title, ICustomTuple customTuple, Type returnType, IBinarySerializer serializer, string extention = null, bool throwIfErrorResponseCode = true)
        {
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));

            if (customTuple == null)
                throw new ArgumentNullException(nameof(customTuple));

            var encoding = NetworkSettings.TitleExtentionEncoding;

            var response = fastRpcClient.Query(
                encoding.GetBytes(title),
                serializer.Serialize(customTuple),
                extention == null ? FrameFormat.EmptyBytes : encoding.GetBytes(extention),
                throwIfErrorResponseCode);

            if (returnType == typeof(void))
                return null;

            if (response.ContentBytes == null)
                return null;

            return serializer.Deserialize(response.ContentBytes, returnType);
        }
    }
}
