using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core
{
    public abstract class TcpTransportHostBase
    {
        internal abstract void ReceiveMessage(TcpTransport tcpTransport, FrameData messageData);

        internal abstract void CloseTransport(TcpTransport tcpTransport);
    }
}
