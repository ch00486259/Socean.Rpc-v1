namespace Socean.Rpc.Core
{
    internal class ReceiveCallbackData
    {
        public void Bind(byte[] buffer, int offset, int size)
        {
            Buffer = buffer;
            Offset = offset;
            Size = size;
        }

        public byte[] Buffer { get; private set; }

        public int Offset { get; private set; }

        public int Size { get; private set; }
    }
}
