using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core
{
    public abstract class ResponseBase
    {
        protected ResponseBase(byte code)
        {
            Code = code;
            Bytes = FrameFormat.EmptyBytes;
        }

        protected ResponseBase(byte[] bytes)
        {
            Bytes = bytes;
        }

        public byte[] Bytes { get; }

        public byte Code { get; }
    }

    public class ErrorResponse : ResponseBase
    {
        public ErrorResponse(byte code) : base(code)
        {

        }
    }

    public class BytesResponse : ResponseBase
    {
        public BytesResponse(byte[] bytes) : base(bytes)
        {

        }
    }

    public class EmptyResponse : ResponseBase
    {
        public EmptyResponse() : base(FrameFormat.EmptyBytes)
        {

        }
    }
}
