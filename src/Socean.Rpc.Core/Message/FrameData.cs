namespace Socean.Rpc.Core.Message
{
    internal class FrameData
    {
        public FrameData(string title, byte[] content, byte stateCode, int messageId)
        {
            Title = title;
            Content = content;
            StateCode = stateCode;
            MessageId = messageId;
        }

        public readonly string Title;

        public readonly byte[] Content;

        public readonly byte StateCode;

        public readonly int MessageId;
    }
}
