namespace Socean.Rpc.Core.Client
{
    public interface IClientFactory
    {
        IClient Create();

        void TakeBack(IClient rpcClient);
    }
}
