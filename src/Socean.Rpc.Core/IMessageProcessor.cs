using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core
{
    public interface IMessageProcessor
    {
        ResponseBase Process(FrameData frameData);
    }
    
}
