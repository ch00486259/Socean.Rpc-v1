using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Socean.Rpc.Core.Message;

namespace Socean.Rpc.Core
{
    internal class TcpTransport : ITransport, IDisposable
    { 
        internal TcpTransport(TcpTransportHostBase transportHost, IPAddress ip, int port )
        {
            RemoteIP = ip;
            RemotePort = port;
            Key = ip + "_" + port;

            _transportHost = transportHost;

            _state = 0;
            _receiveProcessor = new ReceiveProcessor();
        }

        public string Key { get; }
        public IPAddress RemoteIP { get; }
        public int RemotePort { get;  }

        private readonly TcpTransportHostBase _transportHost;

        private readonly ReceiveProcessor _receiveProcessor;
        private Socket _socket;
        private ReceiveCallbackData _tempReceiveCallbackData = new ReceiveCallbackData();

        internal int State
        {
            get { return _state; }
        }

        /// <summary>
        /// 0 未初始化 1 连接 -1 断开连接 
        /// </summary>
        private volatile int _state;

        public void Init(Socket socket = null)
        {
            if (_state != 0)
                return;

            var oldValue = Interlocked.Exchange(ref _state, 1);
            if (oldValue == 1)
                return;

            if (socket == null)
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                var inOptionValues = NetworkSettings.GetClientKeepAliveInfo();
                _socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
                _socket.NoDelay = true;
                _socket.ReceiveTimeout = NetworkSettings.ReceiveTimeout;
                _socket.SendTimeout = NetworkSettings.SendTimeout;
                _socket.Connect(RemoteIP, RemotePort);
            }
            else
            {
                _socket = socket;
                _socket.NoDelay = true;
            }

            BeginReceive();
        }

        public bool? IsSocketConnected
        {
            get
            {
                if (_socket == null)
                    return null;

                return _socket.Connected;
            }
        }

        private void BeginReceive()
        {
            try
            {
                ResetReceiveProcessor();
                BeginNextReceive();
            }
            catch
            {
                Close();
                throw;
            }
        }

        private void ResetReceiveProcessor()
        {
            _receiveProcessor.Reset();
        }

        private void BeginNextReceive()
        {
            _receiveProcessor.GetNextReceiveCallbackData(ref _tempReceiveCallbackData);
            _socket.BeginReceive(_tempReceiveCallbackData.Buffer, _tempReceiveCallbackData.Offset, _tempReceiveCallbackData.Size, SocketFlags.None, ReceiveCallback, this);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            var serverTransport = (TcpTransport)ar.AsyncState;
            if (serverTransport == null)
                return;

            if (serverTransport._state != 1)
                return;

            int sendCount = 0;

            try
            {
                sendCount = serverTransport._socket.EndSend(ar);
            }
            catch
            {

            }

            if (sendCount <= 0)
            {
                serverTransport.Close();
                return;
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            var serverTransport = (TcpTransport)ar.AsyncState;
            if (serverTransport == null)
                return;

            if (serverTransport._state != 1)
                return;

            int readCount = 0;
            try
            {
                readCount = serverTransport._socket.EndReceive(ar);
            }
            catch
            {

            }

            if (readCount <= 0)
            {
                serverTransport.Close();
                return;
            }

            FrameData receiveData = null;

            try
            {
                var receiveProcessor = serverTransport._receiveProcessor;
                receiveProcessor.CheckCurrentReceive(readCount);
                receiveData = receiveProcessor.GetCurrentReceiveData();
                if (receiveData == null)
                {
                    serverTransport.BeginNextReceive();
                    return;
                }

                receiveProcessor.Reset();
                serverTransport.BeginNextReceive();
            }
            catch
            {
                serverTransport.Close();
                return;
            }

            serverTransport.OnReceive(receiveData);
        }

        public void SendAsync(byte[] sendBuffer, int messageByteCount)
        {
            if (_state != 1)
                throw new Exception("send falied,state error");

            try
            {
                _socket.BeginSend(sendBuffer, 0, messageByteCount, SocketFlags.None, SendCallback, this);
            }
            catch
            {
                Close();
                throw;
            }
        }

        public void Send(byte[] sendBuffer, int messageByteCount)
        {
            if (_state != 1)
                throw new Exception("send falied,state error");

            try
            {
                _socket.Send(sendBuffer, messageByteCount, SocketFlags.None);
            }
            catch
            {
                Close();
                throw;
            }
        }

        private void OnReceive(FrameData messageData)
        {
            _transportHost.OnReceiveMessage(this, messageData);
        }

        public void Close()
        {
            if (_state == -1)
                return;

            var oldValue = Interlocked.Exchange(ref _state, -1);
            if (oldValue == -1)
                return;

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {

            }

            try
            {
                _socket.Close();
            }
            catch
            {

            }

            _transportHost.OnTransportClosed(this);
        }

        public void Dispose()
        {
            Close();
        }
    }

    public interface ITransport 
    {
        void Send(byte[] sendBuffer, int messageByteCount);

        void SendAsync(byte[] sendBuffer, int messageByteCount);

        void Close();
    }
}
