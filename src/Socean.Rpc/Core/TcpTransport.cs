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

                //_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 4000);
                //_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 4000);
                //_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                //_socket.LingerState = new System.Net.Sockets.LingerOption(true, 1);

                _socket.Connect(RemoteIP, RemotePort);
            }
            else
            {
                _socket = socket;
                _socket.NoDelay = true;
            }

            BeginNewReceive(true);
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

        private void BeginNewReceive(bool isReset = false)
        {
            try
            {
                if(isReset)
                    _receiveProcessor.Reset();
                _receiveProcessor.GetNextReceiveCallbackData(ref _tempReceiveCallbackData);
                _socket.BeginReceive(_tempReceiveCallbackData.Buffer, _tempReceiveCallbackData.Offset, _tempReceiveCallbackData.Size, SocketFlags.None, ReceiveCallback, this);
            }
            catch(Exception ex)
            {
                LogAgent.Warn("tcpTransport BeginNewReceive,socket BeginReceive error", ex);
                Close();
                throw;
            }
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
            catch(Exception ex)
            {
                LogAgent.Warn("tcpTransport SendCallback,socket EndSend error", ex);                 
            }

            if (sendCount <= 0)
            {
                serverTransport.Close();
                return;
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            var tcpTransport = (TcpTransport)ar.AsyncState;
            if (tcpTransport == null)
                return;

            if (tcpTransport._state != 1)
                return;

            int readCount = -1;
            try
            {
                readCount = tcpTransport._socket.EndReceive(ar);
            }
            catch(Exception ex)
            {
                LogAgent.Warn("tcpTransport ReceiveCallback,socket EndReceive error", ex);
            }

            if (readCount == -1)
            {
                tcpTransport.Close();
                return;
            }

            if (readCount == 0)
            {
                LogAgent.Info("tcpTransport ReceiveCallback,socket receive size:0,remote socket is closed");
                tcpTransport.Close(false);
                return;
            }

            var receiveProcessor = tcpTransport._receiveProcessor;

            try
            {
                var step = receiveProcessor.CheckCurrentStep(readCount);
                if (step == -1)
                    throw new RpcException("tcpTransport ReceiveCallback,receiveProcessor CheckCurrentStep error");

                if(step == 0)
                {
                    tcpTransport.BeginNewReceive(false);
                    return;
                }
            }
            catch(Exception ex)
            {
                tcpTransport.Close();
                LogAgent.Warn("tcpTransport ReceiveCallback,tcpTransport BeginNewReceive error", ex);
                return;
            }


            try
            {
                FrameData receiveData = receiveProcessor.GetCurrentReceiveData();
                tcpTransport.BeginNewReceive(true);
                tcpTransport.OnReceive(receiveData);
            }
            catch (Exception ex)
            {
                tcpTransport.Close();
                LogAgent.Warn("tcpTransport ReceiveCallback,tcpTransport BeginNewReceive error", ex);
                return;
            }
        }

        public void SendAsync(byte[] sendBuffer, int messageByteCount)
        {
            if (_state != 1)
                throw new RpcException("tcpTransport SendAsync,state error");

            try
            {
                _socket.BeginSend(sendBuffer, 0, messageByteCount, SocketFlags.None, SendCallback, this);
            }
            catch(Exception ex)
            {
                Close();
                LogAgent.Warn("tcpTransport SendAsync,socket BeginSend error", ex);
                throw;
            }
        }

        public void Send(byte[] sendBuffer, int messageByteCount)
        {
            if (_state != 1)
                throw new RpcException("tcpTransport send,state error");

            try
            {
                _socket.Send(sendBuffer, messageByteCount, SocketFlags.None);
            }
            catch (Exception ex)
            {
                Close();
                LogAgent.Warn("tcpTransport Send,socket Send error", ex);
                throw;
            }
        }

        private void OnReceive(FrameData messageData)
        {
            _transportHost.OnReceiveMessage(this, messageData);
        }

        public void Close(bool shutDownSocket = true)
        {
            if (_state == -1)
                return;

            var oldValue = Interlocked.Exchange(ref _state, -1);
            if (oldValue == -1)
                return;

            try
            {
                if (shutDownSocket)
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

            _transportHost.OnCloseTransport(this);
        }

        public void Dispose()
        {
            Close();
        }

        internal BytesCache SendBufferCache = new BytesCache(NetworkSettings.WriteBufferSize);        
    }

    internal class BytesCache
    {
        internal BytesCache(int defaultByteLength)
        {
            if (defaultByteLength <= 0)
                throw new RpcException("BytesCache length error");

            _defaultByteLength = defaultByteLength;
            _buffer = new byte[_defaultByteLength];
        }

        private readonly int _defaultByteLength;

        private volatile byte[] _buffer;

        public byte[] Get(int byteCount)
        {
            if (byteCount > _defaultByteLength)
                return new byte[byteCount];

            //var original = Interlocked.Exchange(ref _buffer, null);

            var original = _buffer;
            _buffer = null;

            if (original != null)
                return original;

            return new byte[_defaultByteLength];
        }

        public void Cache(byte[] bytes)
        {
            if (bytes == null)
                return;

            if (bytes.Length != _defaultByteLength)
                return;

            _buffer = bytes;
        }
    }

    public interface ITransport 
    {
        void Send(byte[] sendBuffer, int messageByteCount);

        void SendAsync(byte[] sendBuffer, int messageByteCount);

        void Close(bool onlyCloseSocket);
    }
}
