using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    public sealed class AutoReconnectRpcClient: TransportHostBase,IClient
    {
        public IPAddress ServerIP { get; }
        public int ServerPort { get; }

        private volatile int _messageId = 0;
        private readonly object _queryKey = new object();
        private TcpTransport _transport;
        private QueryContext _queryContext;

        internal AutoReconnectRpcClient(IPAddress ip, int port)
        {
            ServerIP = ip;
            ServerPort = port;

            _transport = new TcpTransport(this, ServerIP, ServerPort);
            _queryContext = new QueryContext();
        }

        private async Task<byte[]> QueryAsync(string title, byte[] contentBytes)
        {
            if (string.IsNullOrEmpty(title))
                throw new Exception();

            if (title.Length > 65535)
                throw new Exception();

            CheckConnection();

            var messageId = Interlocked.Increment(ref _messageId);
            _queryContext.Reset(messageId);
            _transport.Send(title, contentBytes, 0, messageId);
            //_transport.AsyncSend(title, contentBytes, 0, messageId);

            var receiveData = await _queryContext.WaitForResultAsync();
            if (receiveData == null)
            {
                _transport.Close();
                throw new Exception("query timeout");
            }

            var stateCode = receiveData.StateCode;
            if (stateCode != 0)
                throw new Exception("query error:" + stateCode);

            return receiveData.Content;
        }

        public byte[] Query(string title, byte[] contentBytes)
        {
            if (_transport == null)
                throw new Exception("RpcClient has been closed");

            lock (_queryKey)
            {
                return QueryInternal(title, contentBytes);

                //var task = QueryAsync(title, contentBytes);
                //task.Wait();
                //return task.Result;
            }
        }

        private void CheckConnection()
        {
            if (_transport.State == -1)
            {
                try
                {
                    _transport.Close();
                }
                catch  
                {
                 
                }

                _transport = new TcpTransport(this, ServerIP,ServerPort);
                _transport.Init();
            }

            if (_transport.State == 0)
            {
                _transport.Init();
            }
        }

        private byte[] QueryInternal(string title, byte[] contentBytes)
        {
            if (string.IsNullOrEmpty(title))
                throw new Exception();

            if (title.Length > 65535)
                throw new Exception();

            CheckConnection();

            var messageId = Interlocked.Increment(ref _messageId);
            _queryContext.Reset(_messageId);            
            _transport.Send(title, contentBytes, 0, messageId);
            //_transport.AsyncSend(title, contentBytes,0, messageId);

            var receiveData = _queryContext.WaitForResult();
            if (receiveData == null)
            {
                _transport.Close();
                throw new Exception("query timeout");
            }

            var stateCode = receiveData.StateCode;
            if (stateCode != 0)
                throw new Exception("query error:"+ stateCode);

            return receiveData.Content;
        }

        public void Close()
        {
            try
            {
                _transport.Close();
            }
            catch
            {

            }

            _transport = null;
        }

        public void Dispose()
        {
            Close();
        }

        internal override void ReceiveMessage(ITransport tcpTransport, FrameData frameData)
        {
            _queryContext.OnReceive(frameData);
        }

        internal override void CloseTransport(ITransport tcpTransport)
        {

        }
    }
}
