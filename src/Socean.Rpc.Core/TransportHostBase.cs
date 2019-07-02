using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core
{
    public abstract class TransportHostBase
    {
        internal abstract void ReceiveMessage(ITransport tcpTransport, FrameData messageData);

        internal abstract void CloseTransport(ITransport tcpTransport);
    }
}
