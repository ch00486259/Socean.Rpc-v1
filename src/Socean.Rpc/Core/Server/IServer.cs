using System;

namespace Socean.Rpc.Core.Server
{
    public interface IServer: IDisposable
    {
        void Start<T>() where T: IMessageProcessor, new();

        void Close();
    }
}
