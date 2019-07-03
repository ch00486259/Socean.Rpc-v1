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
        private readonly QueryContext _queryContext;

        internal AutoReconnectRpcClient(IPAddress ip, int port)
        {
            ServerIP = ip;
            ServerPort = port;

            _transport = new TcpTransport(this, ServerIP, ServerPort);
            _queryContext = new QueryContext();
        }

        private async Task<FrameData> QueryAsync(string title, byte[] contentBytes,bool throwIfErrorResponseCode = false)
        {
            if (_transport == null)
                throw new Exception("RpcClient has been closed");

            if (string.IsNullOrEmpty(title))
                throw new Exception();

            if (title.Length > 65535)
                throw new Exception();

            lock (_queryKey)
            {
                if (_queryContext.IsCompleted == false)
                    throw new Exception("request is not completed");

                CheckConnection();

                var messageId = Interlocked.Increment(ref _messageId);
                _queryContext.Reset(messageId);

                if (NetworkSettings.TcpRequestSendMode == TcpSendMode.Async)
                    _transport.AsyncSend(title, contentBytes, 0, messageId);
                else
                    _transport.Send(title, contentBytes, 0, messageId);
            }

            var receiveData = await _queryContext.WaitForResultAsync();
            if (receiveData == null)
            {
                _transport.Close();
                throw new Exception("query timeout");
            }

            if (throwIfErrorResponseCode)
            {
                var stateCode = receiveData.StateCode;
                if (stateCode != 0)
                    throw new Exception("query error:" + stateCode);
            }

            return receiveData;
        }

        public FrameData Query(string title, byte[] contentBytes, bool throwIfErrorResponseCode = false)
        {
            if (_transport == null)
                throw new Exception("RpcClient has been closed");

            return QueryInternal(title, contentBytes, throwIfErrorResponseCode);

            //var task = QueryAsync(title, contentBytes, throwIfErrorResponseCode);
            //task.Wait();
            //return task.Result;
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

        private FrameData QueryInternal(string title, byte[] contentBytes, bool throwIfErrorResponseCode)
        {
            if (string.IsNullOrEmpty(title))
                throw new Exception();

            if (title.Length > 65535)
                throw new Exception();

            FrameData receiveData;

            lock (_queryKey)
            {
                if (_queryContext.IsCompleted == false)
                    throw new Exception("request is not completed");

                CheckConnection();

                var messageId = Interlocked.Increment(ref _messageId);
                _queryContext.Reset(_messageId);

                if (NetworkSettings.TcpRequestSendMode == TcpSendMode.Async)
                    _transport.AsyncSend(title, contentBytes, 0, messageId);
                else
                    _transport.Send(title, contentBytes, 0, messageId);

                receiveData = _queryContext.WaitForResult();
                if (receiveData == null)
                {
                    _transport.Close();
                    throw new Exception("query timeout");
                }
            }

            if (throwIfErrorResponseCode)
            {
                var stateCode = receiveData.StateCode;
                if (stateCode != 0)
                    throw new Exception("query error:" + stateCode);
            }

            return receiveData;
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
