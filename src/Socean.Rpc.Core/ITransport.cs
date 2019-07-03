using System;

namespace Socean.Rpc.Core
{
    public interface ITransport : IDisposable
    {
        void Init();

        void Send(string title, byte[] contentBytes, byte stateCode, int messageId);

        void AsyncSend(string title, byte[] contentBytes, byte stateCode, int messageId);

        void Close();
    }
}
