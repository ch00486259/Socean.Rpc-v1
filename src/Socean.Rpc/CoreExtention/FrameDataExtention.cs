using System;

namespace Socean.Rpc.Core.Message
{
    public partial class FrameData
    {
        public string ReadContentAsString()
        {
            if (ContentBytes == null)
                return null;

            return RpcExtentionSettings.DefaultEncoding.GetString(ContentBytes);
        }

        public string ReadTitleAsString()
        {
            if (TitleBytes == null)
                return null;

            return RpcExtentionSettings.DefaultEncoding.GetString(TitleBytes);
        }
    }
}
