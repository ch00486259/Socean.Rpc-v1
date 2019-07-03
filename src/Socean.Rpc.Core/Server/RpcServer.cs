using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core.Server
{
    public sealed class RpcServer: TransportHostBase,IServer
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

        private readonly ConcurrentDictionary<ITransport, int> _transportDictionary = new ConcurrentDictionary<ITransport, int>();
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
            return _transportDictionary.Count;
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

            int backlog = NetworkSettings.ServerListenBacklog;

            try
            {
                _server = new TcpListener(new IPEndPoint(ServerIP, ServerPort));
                _server.ExclusiveAddressUse = true;
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

            client.NoDelay = true;

            var tcpTransport = new TcpTransport(this, ipEndPoint.Address, ipEndPoint.Port, client);
            _transportDictionary.TryAdd(tcpTransport, 0);

            try
            {
                tcpTransport.Init();
            }
            catch
            {
                try
                {
                    tcpTransport.Close();
                    return;
                }
                catch
                {

                }
            }
        }

        private static void ProcessReceive(ITransport serverTransport, FrameData frameData,
            IMessageProcessor messageProcessor)
        {
            byte[] responseContent = null;
            byte responseCode = 0;

            try
            {
                var response = Process(frameData.Title, frameData.ContentBytes, messageProcessor);

                responseContent = response.Bytes;
                responseCode = response.Code;
            }
            catch
            {
                responseCode = ResponseErrorCode.SERVER_INTERNAL_ERROR;
            }

            try
            {
                if (NetworkSettings.TcpRequestSendMode == TcpSendMode.Async)
                {
                    if (responseCode != 0)
                        serverTransport.AsyncSend(string.Empty, FrameFormat.EmptyBytes, responseCode, frameData.MessageId);
                    else
                        serverTransport.AsyncSend(string.Empty, responseContent, 0, frameData.MessageId);
                }
                else
                {
                    if (responseCode != 0)
                        serverTransport.Send(string.Empty, FrameFormat.EmptyBytes, responseCode, frameData.MessageId);
                    else
                        serverTransport.Send(string.Empty, responseContent, 0, frameData.MessageId);
                }
            }
            catch
            {
                serverTransport.Close();
                return;
            }
        }

        private static void ProcessReceiveAsync(ITransport serverTransport, FrameData frameData, IMessageProcessor messageProcessor)
        {
            ThreadPool.QueueUserWorkItem((item) =>
            {
                var tuple = item as Tuple<ITransport, FrameData, IMessageProcessor>;
                var serverTransport1 = tuple.Item1;
                var frameData1 = tuple.Item2;
                var messageProcessor1 = tuple.Item3;

                ProcessReceive(serverTransport1, frameData1, messageProcessor1);

            }, new Tuple<ITransport, FrameData, IMessageProcessor>(serverTransport, frameData, messageProcessor));

            //Task.Run(() =>
            //{
            //    ProcessReceive(serverTransport, frameData, messageProcessor);
            //});
        }

        internal override void ReceiveMessage(ITransport tcpTransport, FrameData frameData)
        {
            ProcessReceiveAsync(tcpTransport, frameData, MessageProcessor);
        }

        internal override void CloseTransport(ITransport transport)
        {
            if (transport == null)
                return;

            _transportDictionary.TryRemove(transport, out var _);
        }

        private static ResponseBase Process(string title, byte[] contentBytes, IMessageProcessor messageProcessor)
        {
            if (messageProcessor == null)
                return new ErrorResponse(ResponseErrorCode.SERVICE_NOT_FOUND);

            if (string.IsNullOrEmpty(title))
                return new ErrorResponse(ResponseErrorCode.SERVICE_TITLE_ERROR);

            return messageProcessor.Process(title, contentBytes);
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

            var transportList = _transportDictionary.Keys.ToList();
            foreach (var transport in transportList)
            {
                try
                {
                    transport.Close();
                }
               catch
                {
                }
            }

            LogAgent.Info("server closed");
        }

        public void Dispose()
        {
            Close();
        }
    }
}
