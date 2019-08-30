using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Server
{
    public sealed class RpcServer: TcpTransportHostBase,IServer
    {
        public RpcServer() 
        {          
            
        }

        public IPAddress ServerIP { get; private set; }
        public int ServerPort { get; private set; }
        public IMessageProcessor MessageProcessor { get; set; }

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

        private TcpListener _server;

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
            if(_serverState != 0)
                throw new Exception();

            var oldValue = Interlocked.Exchange(ref _serverState, 1);
            if(oldValue == 1)
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

                _server = new TcpListener(new IPEndPoint(ServerIP, ServerPort));
                _server.ExclusiveAddressUse = true;
                _server.Server.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
                _server.Start(backlog);
            }
            catch
            {
                _serverState = -1;
                throw;
            }

            try
            {
                _server.BeginAcceptSocket(AcceptSocketCallback, _server);
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

            var server = (TcpListener)ar.AsyncState;

            Socket client = null;
            IPEndPoint ipEndPoint = null;

            try
            {
                client = server.EndAcceptSocket(ar);
                ipEndPoint = (IPEndPoint)client.RemoteEndPoint;
            }
            catch
            {

            }
            
            try
            {
                server.BeginAcceptSocket(AcceptSocketCallback, server);
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

        private static void ProcessReceive(TcpTransport serverTransport, FrameData frameData,
            IMessageProcessor messageProcessor)
        {
            byte[] responseContent = null;
            byte responseCode = 0;

            try
            {
                var response = Process(frameData.Title, frameData.ContentBytes, messageProcessor);

                responseContent = response.Bytes ?? FrameFormat.EmptyBytes;
                responseCode = response.Code;
            }
            catch
            {
                responseContent = FrameFormat.EmptyBytes;
                responseCode = ResponseCode.SERVER_INTERNAL_ERROR;
            }

            try
            {
                if (NetworkSettings.ServerTcpSendMode == TcpSendMode.Async)
                {
                    serverTransport.AsyncSend(string.Empty, responseContent, responseCode, frameData.MessageId);
                }
                else
                {
                    serverTransport.Send(string.Empty, responseContent, responseCode, frameData.MessageId);
                }
            }
            catch
            {
                serverTransport.Close();
                return;
            }
        }

        internal override void ReceiveMessage(TcpTransport serverTransport, FrameData frameData)
        {
            ThreadPool.QueueUserWorkItem((item) =>
            {
                var tuple = item as Tuple<TcpTransport, FrameData, IMessageProcessor>;
                var serverTransport1 = tuple.Item1;
                var frameData1 = tuple.Item2;
                var messageProcessor1 = tuple.Item3;

                ProcessReceive(serverTransport1, frameData1, messageProcessor1);

            }, new Tuple<TcpTransport, FrameData, IMessageProcessor>(serverTransport, frameData, MessageProcessor));

            //Task.Run(() =>
            //{
            //    ProcessReceive(serverTransport, frameData, MessageProcessor);
            //});
        }

        private static ResponseBase Process(string title, byte[] contentBytes, IMessageProcessor messageProcessor)
        {
            if (messageProcessor == null)
                return new ErrorResponse(ResponseCode.SERVICE_NOT_FOUND);

            if (string.IsNullOrEmpty(title))
                return new ErrorResponse(ResponseCode.SERVICE_TITLE_ERROR);

            return messageProcessor.Process(title, contentBytes);
        }

        internal override void CloseTransport(TcpTransport transport)
        {
            if (transport == null)
                return;
        }

        public void Close()
        {
            if (_serverState == 0)
                return;

            if (_serverState == -1)
                return;

            var oldValue = Interlocked.Exchange(ref _serverState, -1);
            if(oldValue == -1)
                return;

            try
            {
                _server.Stop();
            }
            catch
            {

            }

            LogAgent.Info("server closed");
        }

        public void Dispose()
        {
            Close();
        }
    }
}
