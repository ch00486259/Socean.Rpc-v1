using System;

namespace Socean.Rpc.Core.Server
{
    public interface IServer: IDisposable
    {
        void Start();

        void Close();

        IMessageProcessor MessageProcessor { get; set; }
    }
}
