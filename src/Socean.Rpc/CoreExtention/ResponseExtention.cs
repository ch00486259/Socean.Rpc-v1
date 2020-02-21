using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core
{
    public class StringResponse : ResponseBase
    {
        public string Content { get; }

        public string HeaderExtention { get; }

        public StringResponse(string content) : 
            base(FrameFormat.EmptyBytes, 
                RpcExtentionSettings.DefaultEncoding.GetBytes(content ?? string.Empty),
                (byte)ResponseCode.OK)
        {
            Content = content ?? string.Empty;
            HeaderExtention = string.Empty;
        }

        public StringResponse(string content,string entention) : 
            base(RpcExtentionSettings.DefaultEncoding.GetBytes(entention ?? string.Empty),
                RpcExtentionSettings.DefaultEncoding.GetBytes(content ?? string.Empty), 
                (byte)ResponseCode.OK)
        {
            Content = content ?? string.Empty;
            HeaderExtention = entention ?? string.Empty;
        }
    }

    public class ErrorMessageResponse : ResponseBase
    {
        public string Message { get; }

        public ErrorMessageResponse(byte code, string message = null) : base(FrameFormat.EmptyBytes, RpcExtentionSettings.DefaultEncoding.GetBytes(message ?? string.Empty), code)
        {
            Message = message ?? string.Empty;
        }
    }
}
