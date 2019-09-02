using System;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    public interface IClient:IDisposable
    {
        FrameData Query(string title, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = false);

        Task<FrameData> QueryAsync(string title, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = false);

        void Close();
    }
}
