using Socean.Rpc.Core;
using Socean.Rpc.Core.Message;
using System.Threading.Tasks;

namespace Socean.Rpc.DynamicProxy
{
    public abstract class EasyProxyMessageProcessor : IMessageProcessor
    {
        private readonly ServiceHost _serviceHost = new ServiceHost();

        public abstract void Init(IServiceHost serviceHost);

        public void Init()
        {
            Init(_serviceHost);

            _serviceHost.Build();
        }

        public Task<ResponseBase> Process(FrameData frameData)
        {
            if (frameData.TitleBytes == null || frameData.TitleBytes.Length == 0)
                return Task.FromResult<ResponseBase>(new ErrorResponse((byte)ResponseCode.SERVICE_TITLE_ERROR));

            var response = _serviceHost.DoFilterChain(frameData);
            if (response == null)
                return Task.FromResult<ResponseBase>(new EmptyResponse());

            return Task.FromResult(response);
        }
    }
}