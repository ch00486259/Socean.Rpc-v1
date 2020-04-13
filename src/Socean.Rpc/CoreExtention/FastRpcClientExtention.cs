using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    public sealed partial class FastRpcClient
    {
        public byte[] QueryBytes(string title, byte[] contentBytes = null, string extention = null, bool throwIfErrorResponseCode = true)
        {
            var encoding = NetworkSettings.TitleExtentionEncoding;

            var frameData = Query(
                encoding.GetBytes(title),
                contentBytes ?? FrameFormat.EmptyBytes,
                extention == null ? FrameFormat.EmptyBytes : encoding.GetBytes(extention),
                throwIfErrorResponseCode);

            return frameData.ContentBytes;
        }
    }
}
