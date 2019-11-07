using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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
        public IMessageProcessor MessageProcessor { get; set; }
        private ConcurrentDictionary<string, TcpTransport> _clientTransportDictionary = new ConcurrentDictionary<string, TcpTransport>();

        /// <summary>
        /// description：
        /// 0  uninit
        /// 1  running
        /// -1 closed
        ///
        /// sequence：
        ///    0 -> 1 -> -1
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

        public int GetClientCount()
        {
            return 0;
        }

        public void Start()
        {
            if (_serverState != 0)
                throw new Exception();

            var oldValue = Interlocked.Exchange(ref _serverState, 1);
            if (oldValue == 1)
                return;

            if (oldValue == -1)
            {
                _serverState = -1;
                return;
            }

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
            if (_serverState != 1)
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
                LogAgent.Error(ex.Message);
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

                LogAgent.Error(ex.Message);

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

                LogAgent.Error(ex.Message);
                return;
            }

            _clientTransportDictionary[tcpTransport.Key] = tcpTransport;
        }

        internal override void OnReceiveMessage(TcpTransport serverTransport, FrameData frameData)
        {
            ProcessReceive(serverTransport, frameData, MessageProcessor);
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
            if (_serverState == 0)
                return;

            if (_serverState == -1)
                return;

            var oldValue = Interlocked.Exchange(ref _serverState, -1);
            if (oldValue == -1)
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

            LogAgent.Info("server closed");
        }

        public void Dispose()
        {
            Close();
        }
    }
}
