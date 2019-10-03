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
        private volatile bool _isSyncQuery;
        private volatile int _isBusy;

        public SimpleRpcClient(IPAddress ip, int port)
        {
            ServerIP = ip;
            ServerPort = port;

            _transport = new TcpTransport(this, ServerIP, ServerPort);
            _syncQueryContext = new SyncQueryContext();
            _asyncQueryContext = new AsyncQueryContext();
            if (NetworkSettings.LoadTest)
                _asyncQueryContext = new HighResponseQueryContextFacade(_asyncQueryContext);
        }


        private int GetMessageId()
        {
            return (int)(++_messageToken % 100000000);
        }

        private void CheckConnection()
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

            if (_transport.State == -1)
            {
                _transport = new TcpTransport(this, ServerIP, ServerPort);
                _transport.Init();
            }

            if (_transport.State == 0)
            {
                _transport.Init();
            }
        }

        public async Task<FrameData> QueryAsync(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = false)
        {
            if (_transport == null)
                throw new Exception("queryAsync failed,connection has been closed");

            var originalValue = Interlocked.Exchange(ref _isBusy, 1);
            if (originalValue == 1)
                throw new Exception("queryAsync failed,connection is busy");

            try
            {

                return await Task<FrameData>.Run(async () =>
                {
                    return await QueryAsyncInternal(titleBytes, contentBytes, extentionBytes, throwIfErrorResponseCode);

                });
            }
            finally
            {
                _isBusy = 0;
            }
        }

        public async Task<FrameData> QueryAsyncInternal(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = false)
        {
            if (titleBytes == null)
                throw new ArgumentNullException("titleBytes");

            if (titleBytes.Length > 65535)
                throw new ArgumentOutOfRangeException("titleBytes");

            _isSyncQuery = false;

            int messageId = GetMessageId();

            var messageByteCount = FrameFormat.ComputeFrameByteCount(extentionBytes, titleBytes, contentBytes);
            var sendBuffer = _transport.SendBufferCache.Get(messageByteCount);

            FrameFormat.FillFrame(sendBuffer,extentionBytes, titleBytes, contentBytes, 0, messageId);

            _asyncQueryContext.Reset(messageId);

            CheckConnection();

            //if (NetworkSettings.ServerTcpSendMode == TcpSendMode.Async)
            //    _transport.SendAsync(sendBuffer, messageByteCount);
            //else
                _transport.Send(sendBuffer, messageByteCount);

            _transport.SendBufferCache.Cache(sendBuffer);

            var receiveData = await _asyncQueryContext.WaitForResult(messageId, NetworkSettings.ReceiveTimeout);
            
            if (receiveData == null)
            {
                _transport.Close();
                throw new Exception("queryAsync failed, time is out");
            }

            if (throwIfErrorResponseCode)
            {
                var stateCode = receiveData.StateCode;
                if (stateCode != (byte)ResponseCode.OK)
                    throw new Exception("queryAsync failed,error code:" + stateCode);
            }

            return receiveData;
        }

        public FrameData Query(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = false)
        {
            if (_transport == null)
                throw new Exception("query failed,connection has been closed");

            var originalValue = Interlocked.Exchange(ref _isBusy, 1);
            if (originalValue == 1)
                throw new Exception("query failed,connection is busy");

            try
            {
                return QueryInternal(titleBytes, contentBytes, extentionBytes, throwIfErrorResponseCode);
            }
            finally
            {
                _isBusy = 0;
            }
        }

        private FrameData QueryInternal(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes, bool throwIfErrorResponseCode)
        {
            if (titleBytes == null)
                throw new ArgumentNullException("titleBytes");

            if (titleBytes.Length > 65535)
                throw new ArgumentOutOfRangeException("titleBytes");

            _isSyncQuery = true;

            int messageId = GetMessageId();

            var messageByteCount = FrameFormat.ComputeFrameByteCount(extentionBytes, titleBytes, contentBytes);
            var sendBuffer = _transport.SendBufferCache.Get(messageByteCount);

            FrameFormat.FillFrame(sendBuffer,extentionBytes, titleBytes, contentBytes, 0, messageId);

            _syncQueryContext.Reset(messageId);

            CheckConnection();

            //if (NetworkSettings.TcpRequestSendMode == TcpSendMode.Async)
            //    _transport.SendAsync(sendBuffer, messageByteCount);
            //else
                _transport.Send(sendBuffer, messageByteCount);

            _transport.SendBufferCache.Cache(sendBuffer);

            var receiveData = _syncQueryContext.WaitForResult(messageId, NetworkSettings.ReceiveTimeout).Result;
            
            if (receiveData == null)
            {
                _transport.Close();
                throw new Exception("query failed,time is out");
            }

            if (throwIfErrorResponseCode)
            {
                var stateCode = receiveData.StateCode;
                if (stateCode != (byte)ResponseCode.OK)
                    throw new Exception("query failed,error code:" + stateCode);
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
            if (_isSyncQuery)
                _syncQueryContext.OnReceive(frameData);
            else
                _asyncQueryContext.OnReceive(frameData);
        }

        internal override void OnCloseTransport(TcpTransport tcpTransport)
        {

        }
    }
}
