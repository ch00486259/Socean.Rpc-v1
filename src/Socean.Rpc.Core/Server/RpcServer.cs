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
            catch
            {

            }

            try
            {
                server.BeginAccept(AcceptSocketCallback, server);
            }
            catch
            {
                Close();
                return;
            }

            if (client == null)
                return;

            if (ipEndPoint == null)
            {
                try
                {
                    client.Close();
                }
                catch
                {

                }

                return;
            }

            var tcpTransport = new TcpTransport(this, ipEndPoint.Address, ipEndPoint.Port);

            _clientTransportDictionary[ipEndPoint.Address+"_"+ ipEndPoint.Port] = tcpTransport;

            try
            {
                tcpTransport.Init(client);
            }
            catch
            {
                try
                {
                    tcpTransport.Close();
                }
                catch
                {

                }
                return;
            }
        }

        internal override void OnReceiveMessage(TcpTransport serverTransport, FrameData frameData)
        {
            ProcessReceiveAsync(serverTransport, frameData, MessageProcessor);
        }

        private static async Task ProcessReceiveAsync(TcpTransport serverTransport, FrameData frameData,
            IMessageProcessor messageProcessor)
        {
            byte[] responseExtention = null;
            byte[] responseContent = null;
            byte responseCode = 0;

            try
            {
                var response = await ProcessAsync(frameData, messageProcessor);

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
                var tuple = FrameFormat.GenerateFrameBytes(responseExtention, FrameFormat.EmptyBytes, responseContent, responseCode, frameData.MessageId);

                var sendBuffer = tuple.Item1;
                var messageByteCount = tuple.Item2;

                if (NetworkSettings.ServerTcpSendMode == TcpSendMode.Async)
                {
                    serverTransport.SendAsync(sendBuffer, messageByteCount);
                }
                else
                {
                    serverTransport.Send(sendBuffer, messageByteCount);
                }
            }
            catch
            {
                serverTransport.Close();
                return;
            }
        }

        private static async Task<ResponseBase> ProcessAsync(FrameData frameData, IMessageProcessor messageProcessor)
        {
            if (frameData.TitleBytes == null || frameData.TitleBytes.Length == 0)
                return new ErrorResponse((byte)ResponseCode.SERVICE_TITLE_ERROR);

            if (messageProcessor == null)
                return new ErrorResponse((byte)ResponseCode.SERVICE_NOT_FOUND);

            return await messageProcessor.Process(frameData);
        }

        internal override void OnTransportClosed(TcpTransport transport)
        {
            if (transport == null)
                return;

            _clientTransportDictionary.TryRemove(transport.RemoteIP + "_" + transport.RemotePort,out var _) ;
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
