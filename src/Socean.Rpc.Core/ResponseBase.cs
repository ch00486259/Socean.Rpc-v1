using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core
{
    public abstract class ResponseBase
    {
        protected ResponseBase(byte[] extentionBytes, byte[] contentBytes, ResponseCode code)
        {
            Code = code;
            ContentBytes = contentBytes ?? FrameFormat.EmptyBytes;
            HeaderExtentionBytes = extentionBytes ?? FrameFormat.EmptyBytes;
        }

        public byte[] HeaderExtentionBytes { get; }

        public byte[] ContentBytes { get; }

        public ResponseCode Code { get; }
    }

    public class ErrorResponse : ResponseBase
    {
        public ErrorResponse(ResponseCode code) : base(FrameFormat.EmptyBytes, FrameFormat.EmptyBytes, code)
        {

        }
    }

    public class BytesResponse : ResponseBase
    {
        public BytesResponse(byte[] bytes) : base(FrameFormat.EmptyBytes,bytes, ResponseCode.OK)
        {

        }
    }

    public class EmptyResponse : ResponseBase
    {
        public EmptyResponse() : base(FrameFormat.EmptyBytes, FrameFormat.EmptyBytes, ResponseCode.OK)
        {

        }
    }

    public class CustomResponse : ResponseBase
    {
        public CustomResponse(byte[] extentionBytes, byte[] contentBytes, ResponseCode code) : base(extentionBytes, contentBytes, code)
        {

        }
    }
}
