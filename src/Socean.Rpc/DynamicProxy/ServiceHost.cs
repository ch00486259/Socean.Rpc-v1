using Socean.Rpc.Core;
using Socean.Rpc.Core.Message;
using System.Reflection;
using System.Threading.Tasks;

namespace Socean.Rpc.DynamicProxy
{
    public class ServiceHost : IServiceHost
    {
        private readonly FilterChain _filterChain = new FilterChain();
        private readonly FinalProcessFilter _processFilter = new FinalProcessFilter();

        public void RegisterServices(Assembly assembly, IBinarySerializer serializer)
        {
            _processFilter.RegisterServices(assembly, serializer);
        }

        public void RegisterFilter(IServiceFilter filter)
        {
            _filterChain.AddLast(filter);
        }

        internal void Build()
        {
            _filterChain.AddLast(_processFilter);
        }

        internal ResponseBase DoFilterChain(FrameData frameData)
        {
            var response = _filterChain.Do(frameData);
            return response;
        }
    }

    public interface IServiceHost
    {
        void RegisterServices(Assembly assembly, IBinarySerializer serializer);

        void RegisterFilter(IServiceFilter filter);
    }
}
