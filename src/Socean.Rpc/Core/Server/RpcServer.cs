using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Server
{
    public sealed class RpcServer : TcpTransportHostBase, IServer
    {
        public RpcServer()
        {

        }

        public IPAddress ServerIP { get; private set; }
        public int ServerPort { get; private set; }

        private IMessageProcessor _messageProcessor;
        private ConcurrentDictionary<string, TcpTransport> _clientTransportDictionary = new ConcurrentDictionary<string, TcpTransport>();

        /// <summary>
        /// description：
        /// 0  uninit
        /// 1  starting
        /// 2  started
        /// 3  closing
        /// -1 closed
        ///
        /// sequence：
        ///    0 -> 1 -> 2 -> 3 -> -1
        /// </summary>
        private volatile int _serverState = 0;

        private Socket _server;

        public void Bind(IPAddress ip, int port)
        {
            ServerIP = ip;
            ServerPort = port;
        }

        public int ServerState
        {
            get { return _serverState; }
        }

        public void Start<T>() where T: IMessageProcessor, new()
        {
            if (ServerIP == null)
                throw new ArgumentNullException("ServerIP");

            if (ServerPort <= 0 || ServerPort >= 65536)
                throw new ArgumentOutOfRangeException("ServerPort");

            if(_serverState != 0)
                throw new RpcException("rpcserver has started");

            _messageProcessor = new T(); 
            _messageProcessor.Init();

            var originState = Interlocked.CompareExchange(ref _serverState, 1, 0);
            if (originState != 0)
                throw new RpcException("rpcserver has started");

            try
            {
                var inOptionValues = NetworkSettings.GetServerKeepAliveInfo();
                var backlog = NetworkSettings.ServerListenBacklog;

                _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _server.ExclusiveAddressUse = true;
                _server.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
                _server.LingerState = new System.Net.Sockets.LingerOption(true, 0);

                _server.Bind(new IPEndPoint(ServerIP, ServerPort));
                _server.Listen(backlog);
            }
            catch
            {
                _serverState = -1;
                throw;
            }

            var originState2 = Interlocked.CompareExchange(ref _serverState, 2,1);
            if (originState2 != 1)
                return;

            try
            {
                _server.BeginAccept(AcceptSocketCallback, _server);
            }
            catch
            {
                Close();
                return;
            }
        }

        private void AcceptSocketCallback(IAsyncResult ar)
        {
            if (_serverState != 2)
                return;

            var server = (Socket)ar.AsyncState;

            Socket client = null;
            IPEndPoint ipEndPoint = null;

            try
            {
                client = server.EndAccept(ar);
                ipEndPoint = (IPEndPoint)client.RemoteEndPoint;
            }
            catch(Exception ex)
            {
                LogAgent.Warn(ex.Message);
            }

            try
            {
                server.BeginAccept(AcceptSocketCallback, server);
            }
            catch(Exception ex)
            {
                Close();
                try
                {
                    if (client != null)
                        client.Close();
                }
                catch
                {

                }

                LogAgent.Warn(ex.Message);
                return;
            }

            if (ipEndPoint == null)
            {
                try
                {
                    if (client != null)
                        client.Close();
                }
                catch
                {

                }

                return;
            }

            var tcpTransport = new TcpTransport(this, ipEndPoint.Address, ipEndPoint.Port);

            try
            {
                tcpTransport.Init(client);
            }
            catch(Exception ex)
            {
                try
                {
                    tcpTransport.Close();
                }
                catch
                {

                }

                LogAgent.Warn("close network in tcpTransport Init", ex);
                return;
            }

            _clientTransportDictionary[tcpTransport.Key] = tcpTransport;
        }

        internal override void OnReceiveMessage(TcpTransport serverTransport, FrameData frameData)
        {
            ProcessReceive(serverTransport, frameData, _messageProcessor);
        }

        private static async void ProcessReceive(TcpTransport serverTransport, FrameData frameData,
           IMessageProcessor messageProcessor)
        {
            byte[] responseExtention = null;
            byte[] responseContent = null;
            byte responseCode = 0;

            try
            {
                ResponseBase response = null;

                if (frameData.TitleBytes == null || frameData.TitleBytes.Length == 0)
                    response = new ErrorResponse((byte)ResponseCode.SERVICE_TITLE_ERROR);

                if (messageProcessor == null)
                    response = new ErrorResponse((byte)ResponseCode.SERVICE_NOT_FOUND);

                if (response == null)
                {
                    if (NetworkSettings.ServerProcessMode == CommunicationMode.Sync)
                    {
                        var responseTask = messageProcessor.Process(frameData);
                        responseTask.Wait();
                        response = responseTask.Result;
                    }
                    else
                    { 
                        response = await messageProcessor.Process(frameData);  
                    }
                }

                responseExtention = response.HeaderExtentionBytes ?? FrameFormat.EmptyBytes;
                responseContent = response.ContentBytes ?? FrameFormat.EmptyBytes;
                responseCode = (byte)response.Code;
            }
            catch
            {
                responseExtention = FrameFormat.EmptyBytes;
                responseContent = FrameFormat.EmptyBytes;
                responseCode = (byte)ResponseCode.SERVER_INTERNAL_ERROR;
            }

            try
            {
                var messageByteCount = FrameFormat.ComputeFrameByteCount(responseExtention, FrameFormat.EmptyBytes, responseContent);
                var sendBuffer = serverTransport.SendBufferCache.Get(messageByteCount);

                FrameFormat.FillFrame(sendBuffer, responseExtention, FrameFormat.EmptyBytes, responseContent, responseCode, frameData.MessageId);

                //if (NetworkSettings.ServerTcpSendMode == TcpSendMode.Async)
                //{
                //    serverTransport.SendAsync(sendBuffer, messageByteCount);
                //}
                //else
                //{
                serverTransport.Send(sendBuffer, messageByteCount);
                //}

                serverTransport.SendBufferCache.Cache(sendBuffer);
            }
            catch
            {
                serverTransport.Close();
            }
        }

        internal override void OnCloseTransport(TcpTransport transport)
        {
            if (transport == null)
                return;

            _clientTransportDictionary.TryRemove(transport.Key,out var _) ;
        }

        public void Close()
        {
            var originState = Interlocked.CompareExchange(ref _serverState, 3, 2);
            if (originState != 2)
                return;

            try
            {
                _server.Close();
            }
            catch
            {

            }

            var clientTransportList = _clientTransportDictionary.Values.ToList();
            foreach (var transport in clientTransportList)
            {
                transport.Close();
            }

            _serverState = -1;

            LogAgent.Info("server closed");
        }

        public void Dispose()
        {
            Close();
        }
    }
}
