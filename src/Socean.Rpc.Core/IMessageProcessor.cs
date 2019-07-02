namespace Socean.Rpc.Core
{
    public interface IMessageProcessor
    {
        ResponseBase Process(string title, byte[] contentBytes);
    }
    
}
