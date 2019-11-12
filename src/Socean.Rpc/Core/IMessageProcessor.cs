using Socean.Rpc.Core.Message;
using System.Threading.Tasks;

namespace Socean.Rpc.Core
{
    public interface IMessageProcessor
    {
        void Init();

        Task<ResponseBase> Process(FrameData frameData);
    }
}
