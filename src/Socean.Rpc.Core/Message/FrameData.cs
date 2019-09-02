namespace Socean.Rpc.Core.Message
{
    public class FrameData
    {
        public FrameData(byte[] extention,string title, byte[] contentBytes, byte stateCode, int messageId)
        {
            HeaderExtention = extention;
            Title = title;
            ContentBytes = contentBytes;
            StateCode = stateCode;
            MessageId = messageId;
        }

        public readonly byte[] HeaderExtention;

        public readonly string Title;

        public readonly byte[] ContentBytes;

        public readonly byte StateCode;

        public readonly int MessageId;
    }
}
