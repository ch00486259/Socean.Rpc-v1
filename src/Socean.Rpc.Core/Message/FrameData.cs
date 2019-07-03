namespace Socean.Rpc.Core.Message
{
    public class FrameData
    {
        public FrameData(string title, byte[] contentBytes, byte stateCode, int messageId)
        {
            Title = title;
            ContentBytes = contentBytes;
            StateCode = stateCode;
            MessageId = messageId;
        }

        public readonly string Title;

        public readonly byte[] ContentBytes;

        public readonly byte StateCode;

        public readonly int MessageId;
    }
}
