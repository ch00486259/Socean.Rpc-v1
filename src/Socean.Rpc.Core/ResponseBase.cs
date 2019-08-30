using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core
{
    public abstract class ResponseBase
    {
        protected ResponseBase(byte[] extentionBytes, byte[] contentBytes, byte code)
        {
            Code = code;
            ContentBytes = contentBytes ?? FrameFormat.EmptyBytes;
            ExtentionBytes = extentionBytes ?? FrameFormat.EmptyBytes;
        }

        public byte[] ExtentionBytes { get; }

        public byte[] ContentBytes { get; }

        public byte Code { get; }
    }

    public class ErrorResponse : ResponseBase
    {
        public ErrorResponse(byte code) : base(FrameFormat.EmptyBytes, FrameFormat.EmptyBytes, code)
        {

        }
    }

    public class BytesResponse : ResponseBase
    {
        public BytesResponse(byte[] bytes) : base(FrameFormat.EmptyBytes,bytes,ResponseCode.OK)
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
        public CustomResponse(byte[] extentionBytes, byte[] contentBytes, byte code) : base(extentionBytes, contentBytes, code)
        {

        }
    }
}
