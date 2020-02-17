using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core
{
    public class StringResponse : ResponseBase
    {
        public StringResponse(string value) : base(FrameFormat.EmptyBytes, RpcExtentionSettings.DefaultEncoding.GetBytes(value), (byte)ResponseCode.OK)
        {

        }
    }
}
