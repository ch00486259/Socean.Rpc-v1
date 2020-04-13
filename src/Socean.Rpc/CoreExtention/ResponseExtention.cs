using Socean.Rpc.Core.Message;
using System.Text;

namespace Socean.Rpc.Core
{
    public class StringResponse : ResponseBase
    {
        public string Content { get; }

        public string HeaderExtention { get; }

        public StringResponse(string content,Encoding contentEncoding) :
            base(FrameFormat.EmptyBytes,
                contentEncoding.GetBytes(content ?? string.Empty),
                (byte)ResponseCode.OK)
        {
            Content = content ?? string.Empty;
            HeaderExtention = string.Empty;
        }

        public StringResponse(string content,Encoding contentEncoding, string entention) :
            base(NetworkSettings.TitleExtentionEncoding.GetBytes(entention ?? string.Empty),
                contentEncoding.GetBytes(content ?? string.Empty),
                (byte)ResponseCode.OK)
        {
            Content = content ?? string.Empty;
            HeaderExtention = entention ?? string.Empty;
        }
    }
}
