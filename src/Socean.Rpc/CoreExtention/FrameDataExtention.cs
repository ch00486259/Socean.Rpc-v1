using System;

namespace Socean.Rpc.Core.Message
{
    public partial class FrameData
    {
        public string ReadTitleAsString()
        {
            if (TitleBytes == null)
                return null;

            return NetworkSettings.TitleExtentionEncoding.GetString(TitleBytes);
        }

        public string ReadExtentionAsString()
        {
            if (HeaderExtentionBytes == null)
                return null;

            return NetworkSettings.TitleExtentionEncoding.GetString(HeaderExtentionBytes);
        }
    }
}
