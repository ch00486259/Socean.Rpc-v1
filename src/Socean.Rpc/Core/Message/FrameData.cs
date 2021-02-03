namespace Socean.Rpc.Core.Message
{
    internal class AsyncFrameDataFacade
    {
        internal FrameData FrameData { get; set; }
    }

    public partial class FrameData
    {
        public FrameData(byte[] headerExtentionBytes, byte[] titleBytes, byte[] contentBytes, byte stateCode, int messageId)
        {
            HeaderExtentionBytes = headerExtentionBytes;
            TitleBytes = titleBytes;
            ContentBytes = contentBytes;
            StateCode = stateCode;
            MessageId = messageId;
        }

        public readonly byte[] HeaderExtentionBytes;

        public readonly byte[] TitleBytes;

        public readonly byte[] ContentBytes;

        public readonly byte StateCode;

        public readonly int MessageId;
    }
}
