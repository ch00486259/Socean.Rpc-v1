using System;
using System.Runtime.InteropServices;

namespace Socean.Rpc.Core
{
    public static class NetworkSettings
    {
        private static int _clientDetectReceiveInterval = 5;
        private static int _receiveTimeout = 1000 * 10;
        private static int _sendTimeout = 1000 * 10;
        private static int _writeBufferSize = 4096;
        private static int _readBufferSize = 4096;
        private static int _clientCacheSize = 3;
        private static int _serverListenBacklog = 50000;
        private static int _reconnectInterval = 5000;
        private static CommunicationMode _serverTcpSendMode = CommunicationMode.Sync;

        public static int ClientDetectReceiveInterval
        {
            get { return _clientDetectReceiveInterval;}
            set
            {
                if (value < 0)
                    throw new Exception();

                _clientDetectReceiveInterval = value;
            }
        }

        public static int ReceiveTimeout
        {
            get { return _receiveTimeout;}
            set
            {
                if (value <= 0)
                    throw new Exception();

                _receiveTimeout = value;
            }
        }

        public static int SendTimeout
        {
            get { return _sendTimeout;}
            set
            {
                if (value <= 0)
                    throw new Exception();

                _sendTimeout = value;
            }
        }

        public static int WriteBufferSize
        {
            get { return _writeBufferSize;}
            set
            {
                if (value < 200)
                    throw new Exception();

                _writeBufferSize = value;
            }
        }

        public static int ReadBufferSize
        {
            get { return _readBufferSize;}
            set
            {
                if (value < 200)
                    throw new Exception();

                _readBufferSize = value;
            }
        }

        public static int ClientCacheSize
        {
            get { return _clientCacheSize;}
            set
            {
                if (value < 0)
                    throw new Exception();

                _clientCacheSize = value;
            }
        }

        public static int ReconnectInterval
        {
            get { return _reconnectInterval;}
            set
            {
                if (value <= 0)
                    throw new Exception();

                _reconnectInterval = value;
            }
        }

        public static int ServerListenBacklog
        {
            get { return _serverListenBacklog;}
            set
            {
                if (value <= 0)
                    throw new Exception();

                _serverListenBacklog = value;
            }
        }

        public static CommunicationMode ServerTcpSendMode
        {
            get { return _serverTcpSendMode;}
            set { _serverTcpSendMode = value; }
        }

        public static bool LoadTest { get; set; }

        public static CommunicationMode ServerProcessMode { get; set; } = CommunicationMode.Async;

        public static byte[] GetClientKeepAliveInfo()
        {
            var uintSize = Marshal.SizeOf(0);
            byte[] inOptionValues = new byte[uintSize * 3];
            BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)120000).CopyTo(inOptionValues, Marshal.SizeOf(uintSize));
            BitConverter.GetBytes((uint)2000).CopyTo(inOptionValues, Marshal.SizeOf(uintSize) * 2);

            return inOptionValues;
        }

        public static byte[] GetServerKeepAliveInfo()
        {
            var uintSize = Marshal.SizeOf(0);
            byte[] inOptionValues = new byte[uintSize * 3];
            BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)600000).CopyTo(inOptionValues, Marshal.SizeOf(uintSize));
            BitConverter.GetBytes((uint)2000).CopyTo(inOptionValues, Marshal.SizeOf(uintSize) * 2);

            return inOptionValues;
        }
    }

    public enum CommunicationMode
    {
        Sync = 0,
        Async = 1
    }
}
