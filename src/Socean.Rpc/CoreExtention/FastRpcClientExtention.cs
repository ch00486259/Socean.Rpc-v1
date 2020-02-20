using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    public sealed partial class FastRpcClient
    {
        public string QueryString(string title, string content = null, string extention = null, bool throwIfErrorResponseCode = false)
        {
            var encoding = RpcExtentionSettings.DefaultEncoding;

            var frameData = Query(
                encoding.GetBytes(title),
                content == null ? FrameFormat.EmptyBytes : encoding.GetBytes(content),
                extention == null ? FrameFormat.EmptyBytes : encoding.GetBytes(extention),
                throwIfErrorResponseCode);

            return frameData.ReadContentString();
        }

        public byte[] QueryBytes(string title, string content = null, string extention = null, bool throwIfErrorResponseCode = false)
        {
            var encoding = RpcExtentionSettings.DefaultEncoding;

            var frameData = Query(
                encoding.GetBytes(title),
                content == null ? FrameFormat.EmptyBytes : encoding.GetBytes(content),
                extention == null ? FrameFormat.EmptyBytes : encoding.GetBytes(extention),
                throwIfErrorResponseCode);

            return frameData.ContentBytes;
        }
    }
}
