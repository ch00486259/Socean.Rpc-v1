using Socean.Rpc.Core.Message;
using Socean.Rpc.DynamicProxy;
using System;

namespace Socean.Rpc.Core.Client
{
    public sealed partial class FastRpcClient
    {
        internal object QueryInternal(string title, ICustomTuple customTuple, Type returnType, IRpcSerializer rpcSerializer, string extention = null, bool throwIfErrorResponseCode = false)
        {
            if (rpcSerializer == null)
                throw new ArgumentNullException(nameof(rpcSerializer));

            if (customTuple == null)
                throw new ArgumentNullException(nameof(customTuple));

            var encoding = DynamicProxySettings.DefaultEncoding;

            var response = Query(
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

        public string QueryString(string title, string content = null, string extention = null, bool throwIfErrorResponseCode = false)
        {
            var encoding = DynamicProxySettings.DefaultEncoding;

            var frameData = Query(
                encoding.GetBytes(title),
                content == null ? FrameFormat.EmptyBytes : encoding.GetBytes(content),
                extention == null ? FrameFormat.EmptyBytes : encoding.GetBytes(extention),
                throwIfErrorResponseCode);

            return frameData.ReadContentString();
        }

        public byte[] QueryBytes(string title, string content = null, string extention = null, bool throwIfErrorResponseCode = false)
        {
            var encoding = DynamicProxySettings.DefaultEncoding;

            var frameData = Query(
                encoding.GetBytes(title),
                content == null ? FrameFormat.EmptyBytes : encoding.GetBytes(content),
                extention == null ? FrameFormat.EmptyBytes : encoding.GetBytes(extention),
                throwIfErrorResponseCode);

            return frameData.ContentBytes;
        }
    }
}
