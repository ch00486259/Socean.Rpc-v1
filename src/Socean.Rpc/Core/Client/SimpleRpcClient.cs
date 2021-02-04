using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Client
{   
    public sealed class SimpleRpcClient: TcpTransportHostBase, IClient
    {
        public IPAddress ServerIP { get; }
        public int ServerPort { get; }

        private readonly RequestMessageConstructor _requestMessageConstructor;
        private readonly TcpTransport _transport;
        private readonly SyncQueryContext _syncQueryContext;
        private readonly IAsyncQueryContext _asyncQueryContext;

        internal bool? IsSocketConnected { get { return _transport.IsSocketConnected; } }
        internal TcpTransportState TransportState { get { return _transport.State; } }

        /// <summary>
        /// 0 idle, 1 sync, 2 async
        /// </summary>
        private volatile int _queryState;

        private readonly AsyncFrameDataFacade _asyncFrameDataFacade = new AsyncFrameDataFacade();


        public SimpleRpcClient(IPAddress ip, int port)
        {
            ServerIP = ip;
            ServerPort = port;

            _transport = new TcpTransport(this, ServerIP, ServerPort);
            _requestMessageConstructor = new RequestMessageConstructor();
            _syncQueryContext = new SyncQueryContext();
            _asyncQueryContext = new HighResponseAsyncQueryContext();
        }

        private void CheckTransport()
        {
            var transportState = _transport.State;
            if (transportState == TcpTransportState.Closed)
                throw new RpcException("SimpleRpcClient CheckTransport failed,connection has been closed");

            if (transportState == TcpTransportState.Uninit)
            {
                try
                {
                    _transport.Init();
                }
                catch(Exception ex)
                {
                    try
                    {
                        _transport.Close();
                    }
                    catch
                    {

                    }

                    LogAgent.Warn("close network in SimpleRpcClient CheckTransport,transport init error", ex);
                    throw;
                }
            }
        }

        public async Task<FrameData> QueryAsync(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = true)
        {
            if (titleBytes == null)
                throw new ArgumentNullException("titleBytes");

            if (titleBytes.Length > 65535)
                throw new ArgumentOutOfRangeException("titleBytes");

            var originalQueryState = Interlocked.CompareExchange(ref _queryState, 2, 0);
            if (originalQueryState != 0)
                throw new RpcException("SimpleRpcClient QueryAsync failed,client is querying");

            try
            {
                CheckTransport();

                _requestMessageConstructor.ConstructCurrentMessage(titleBytes, contentBytes, extentionBytes, throwIfErrorResponseCode);
                await QueryAsyncInternal(_requestMessageConstructor, _asyncFrameDataFacade).ConfigureAwait(false);
                var frameData = _asyncFrameDataFacade.FrameData;
                _requestMessageConstructor.ClearCurrentMessage();

                return frameData;
            }
            finally
            {
                _queryState = 0;
            }
        }
               
        private async Task QueryAsyncInternal(RequestMessageConstructor rmc, AsyncFrameDataFacade asyncFrameDataFacade)
        {
            _asyncQueryContext.Reset();
            _transport.SendAsync(rmc.SendBuffer, rmc.MessageByteCount);            
            await _asyncQueryContext.WaitForResult(NetworkSettings.ReceiveTimeout, asyncFrameDataFacade).ConfigureAwait(false);
            var receiveData = asyncFrameDataFacade.FrameData;
            if (receiveData == null)
            {
                //_transport.Close();
                throw new RpcException("SimpleRpcClient QueryAsync failed, time is out");
            }

            if (rmc.ThrowIfErrorResponseCode)
            {
                if (receiveData.StateCode != (byte)ResponseCode.OK)
                    throw new RpcException(string.Format("SimpleRpcClient QueryAsync failed,error code:{0},message:{1}",
                       receiveData.StateCode,
                       NetworkSettings.ErrorContentEncoding.GetString(receiveData.ContentBytes ?? FrameFormat.EmptyBytes)));
            }

            return ;
        }

        public FrameData Query(byte[] titleBytes, byte[] contentBytes, byte[] extentionBytes = null, bool throwIfErrorResponseCode = true)
        {
            if (titleBytes == null)
                throw new ArgumentNullException("titleBytes");

            if (titleBytes.Length > 65535)
                throw new ArgumentOutOfRangeException("titleBytes");

            var originalQueryState = Interlocked.CompareExchange(ref _queryState, 1, 0);
            if (originalQueryState != 0)
                throw new RpcException("SimpleRpcClient Query failed,client is querying");

            try
            {
                CheckTransport();

                _requestMessageConstructor.ConstructCurrentMessage(titleBytes, contentBytes, extentionBytes, throwIfErrorResponseCode);
                var frameData = QueryInternal(_requestMessageConstructor);
                _requestMessageConstructor.ClearCurrentMessage();

                return frameData;
            }
            finally
            {
                _queryState = 0;
            }
        }

        private FrameData QueryInternal(RequestMessageConstructor rmc)
        {
            _syncQueryContext.Reset();                 
            _transport.Send(rmc.SendBuffer, rmc.MessageByteCount);
            var receiveData = _syncQueryContext.WaitForResult(NetworkSettings.ReceiveTimeout);            
            if (receiveData == null)
            {
                //_transport.Close();
                throw new RpcException("SimpleRpcClient Query failed,time is out");
            }

            if (rmc.ThrowIfErrorResponseCode)
            {
                if (receiveData.StateCode != (byte)ResponseCode.OK)
                    throw new RpcException(string.Format("SimpleRpcClient Query failed,error code:{0},message:{1}", 
                        receiveData.StateCode,
                        NetworkSettings.ErrorContentEncoding.GetString(receiveData.ContentBytes ?? FrameFormat.EmptyBytes)));
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

            try
            {
                _asyncQueryContext.Dispose();
            }
            catch
            {

            }
        }

        public void Dispose()
        {
            Close();
        }

        internal override void OnReceiveMessage(TcpTransport tcpTransport, FrameData frameData)
        {
            if (frameData == null)
                return;

            if (frameData.MessageId != _requestMessageConstructor.MessageId)
                return;

            if (_queryState == 1)
                _syncQueryContext.OnReceiveResult(frameData);

            if (_queryState == 2)
                _asyncQueryContext.OnReceiveResult(frameData);
        }

        internal override void OnCloseTransport(TcpTransport tcpTransport)
        {

        }
    }
}
