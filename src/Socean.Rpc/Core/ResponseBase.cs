using Socean.Rpc.Core.Message;
using System.Text;

namespace Socean.Rpc.Core
{
    public abstract class ResponseBase
    {
        protected ResponseBase(byte[] headerExtentionBytes, byte[] contentBytes, byte code)
        {
            Code = code;
            ContentBytes = contentBytes ?? FrameFormat.EmptyBytes;
            HeaderExtentionBytes = headerExtentionBytes ?? FrameFormat.EmptyBytes;
        }

        public byte[] HeaderExtentionBytes { get; protected set; }

        public byte[] ContentBytes { get; protected set; }

        public byte Code { get; protected set; }
    }

    public class ErrorResponse : ResponseBase
    {
        public ErrorResponse(byte code) : base(FrameFormat.EmptyBytes, FrameFormat.EmptyBytes, code)
        {

        }
    }

    public class BytesResponse : ResponseBase
    {
        public BytesResponse(byte[] contentBytes) : base(FrameFormat.EmptyBytes, contentBytes, (byte)ResponseCode.OK)
        {

        }

        public BytesResponse(byte[] contentBytes,byte[] extentionBytes) : base(extentionBytes, contentBytes, (byte)ResponseCode.OK)
        {

        }
    }

    public class EmptyResponse : ResponseBase
    {
        public EmptyResponse() : base(FrameFormat.EmptyBytes, FrameFormat.EmptyBytes, (byte)ResponseCode.OK)
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
