using Socean.Rpc.Core.Message;
using System.Threading.Tasks;

namespace Socean.Rpc.Core
{
    public interface IMessageProcessor
    {
        Task<ResponseBase> Process(FrameData frameData);
    }
}
