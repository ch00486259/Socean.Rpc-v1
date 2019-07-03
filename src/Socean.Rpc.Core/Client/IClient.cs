using System;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    public interface IClient:IDisposable
    {
        FrameData Query(string title, byte[] contentBytes,bool throwIfErrorResponseCode = false);

        void Close();
    }
}
