using System;

namespace Socean.Rpc.Core.Client
{
    public interface IClient:IDisposable
    {
        byte[] Query(string title, byte[] contentBytes);

        void Close();
    }
}
