namespace Socean.Rpc.Core
{
    public interface IKeepAlive
    {
        void CheckConnection();

        bool AutoReconnect { get; set; }
    }
}
