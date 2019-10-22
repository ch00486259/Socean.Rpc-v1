using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core
{
    public abstract class TcpTransportHostBase
    {
        internal abstract void OnReceiveMessage(TcpTransport tcpTransport, FrameData messageData);

        internal abstract void OnCloseTransport(TcpTransport tcpTransport);
    }
}
