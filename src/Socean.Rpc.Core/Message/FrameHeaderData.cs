using System;

namespace Socean.Rpc.Core.Message
{
    internal class FrameHeaderData
    {
        internal void Bind(Int16 titleLength, int contentLength, byte stateCode, int messageId)
        {
            this.TitleLength = titleLength;
            this.ContentLength = contentLength;
            this.StateCode = stateCode;
            this.MessageId = messageId;
        }

        public Int16 TitleLength { get; private set; }

        public int ContentLength { get; private set; }

        public byte StateCode { get; private set; }

        public int MessageId { get; private set; }
    }
}
