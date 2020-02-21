using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{
    public sealed class SimpleRpcClient: TcpTransportHostBase,IClient
    {
        public IPAddress ServerIP { get; }
        public int ServerPort { get; }

        private volatile uint _messageToken = 0;
        private volatile TcpTransport _transport;
        private readonly IQueryContext _syncQueryContext;
        private readonly IQueryContext _asyncQueryContext;

        /// <summary>
        /// 0 idle,1 sync, 2 async
        /// </summary>
        private volatile int _stateCode;

        public SimpleRpcClient(IPAddress ip, int port)
        {
            ServerIP = ip;
            ServerPort = port;

            _transport = new TcpTransport(this, ServerIP, ServerPort);
            _syncQueryContext = new SyncQueryContext();
            _asyncQueryContext = new AsyncQueryContext();
        }

        private int GenerateMessageId()
        {
            return (int)(++_messageToken % 100000000);
        }

        private void TransportKeepAlive()
        {
            if (_transport.IsSocketConnected == false)
            {
                try
                {
                    _transport.Close();
                }
                catch
                {

                }
            }

            var transportState = _transport.State;
            if (transportState == -1)
            {
                _transport = new TcpTransport(this, ServerIP, ServerPort);
                _transport.Init();
            }
            if (transportState == 0)
            {
                _transport.Init();
            }
        }

        public async Task<FrameData> QueryAsync(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = true)
        {
            if (_transport == null)
                throw new RpcException("queryAsync failed,connection has been closed");

            var originalValue = Interlocked.Exchange(ref _stateCode, 2);
            if (originalValue != 0)
                throw new RpcException("queryAsync failed,connection is busy");

            try
            {
                return await QueryAsyncInternal(titleBytes, contentBytes, extentionBytes, throwIfErrorResponseCode);
            }
            finally
            {
                _stateCode = 0;
            }
        }
               
        private async Task<FrameData> QueryAsyncInternal(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = true)
        {
            if (titleBytes == null)
                throw new ArgumentNullException("titleBytes");

            if (titleBytes.Length > 65535)
                throw new ArgumentOutOfRangeException("titleBytes");

            int messageId = GenerateMessageId();
            var messageByteCount = FrameFormat.ComputeFrameByteCount(extentionBytes, titleBytes, contentBytes);
            var sendBuffer = _transport.SendBufferCache.Get(messageByteCount);
            FrameFormat.FillFrame(sendBuffer, extentionBytes, titleBytes, contentBytes, 0, messageId);

            TransportKeepAlive();

            _asyncQueryContext.Reset(messageId);
            _transport.SendAsync(sendBuffer, messageByteCount);            
            var receiveData = await _asyncQueryContext.WaitForResult(messageId, NetworkSettings.ReceiveTimeout);
            if (receiveData == null)
            {
                _transport.Close();
                throw new RpcException("queryAsync failed, time is out");
            }

            _transport.SendBufferCache.Cache(sendBuffer);

            if (throwIfErrorResponseCode)
            {
                var stateCode = receiveData.StateCode;
                if (stateCode != (byte)ResponseCode.OK)
                    throw new RpcException("queryAsync failed,error code:" + stateCode);
            }

            return receiveData;
        }

        public FrameData Query(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = true)
        {
            if (_transport == null)
                throw new RpcException("query failed,connection has been closed");

            var originalValue = Interlocked.Exchange(ref _stateCode, 1);
            if (originalValue != 0)
                throw new RpcException("query failed,connection is busy");

            try
            {
                return QueryInternal(titleBytes, contentBytes, extentionBytes, throwIfErrorResponseCode);
            }
            finally
            {
                _stateCode = 0;
            }
        }

        private FrameData QueryInternal(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes, bool throwIfErrorResponseCode)
        {
            if (titleBytes == null)
                throw new ArgumentNullException("titleBytes");

            if (titleBytes.Length > 65535)
                throw new ArgumentOutOfRangeException("titleBytes");

            int messageId = GenerateMessageId();
            var messageByteCount = FrameFormat.ComputeFrameByteCount(extentionBytes, titleBytes, contentBytes);
            var sendBuffer = _transport.SendBufferCache.Get(messageByteCount);
            FrameFormat.FillFrame(sendBuffer,extentionBytes, titleBytes, contentBytes, 0, messageId);

            TransportKeepAlive();

            _syncQueryContext.Reset(messageId);
            _transport.Send(sendBuffer, messageByteCount);
            _transport.SendBufferCache.Cache(sendBuffer);
            var receiveData = _syncQueryContext.WaitForResult(messageId, NetworkSettings.ReceiveTimeout).Result;            
            if (receiveData == null)
            {
                _transport.Close();
                throw new RpcException("query failed,time is out");
            }

            if (throwIfErrorResponseCode)
            {
                var stateCode = receiveData.StateCode;
                if (stateCode != (byte)ResponseCode.OK)
                    throw new RpcException("query failed,error code:" + stateCode);
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

        internal override void OnReceiveMessage(TcpTransport tcpTransport, FrameData frameData)
        {
            if (_stateCode == 1)
                _syncQueryContext.OnReceiveResult(frameData);

            if (_stateCode == 2)
                _asyncQueryContext.OnReceiveResult(frameData);
        }

        internal override void OnCloseTransport(TcpTransport tcpTransport)
        {

        }
    }
}
