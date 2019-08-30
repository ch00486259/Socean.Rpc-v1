namespace Socean.Rpc.Core.Message
{
    public class FrameData
    {
        public FrameData(byte[] extention,string title, byte[] contentBytes, byte stateCode, int messageId)
        {
            Extention = extention;
            Title = title;
            ContentBytes = contentBytes;
            StateCode = stateCode;
            MessageId = messageId;
        }

        public readonly byte[] Extention;

        public readonly string Title;

        public readonly byte[] ContentBytes;

        public readonly byte StateCode;

        public readonly int MessageId;
    }
}
