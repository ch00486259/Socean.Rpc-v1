using System;

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

        public static int ClientDetectReceiveInterval
        {
            get => _clientDetectReceiveInterval;
            set
            {
                if (value < 0)
                    throw new Exception();

                _clientDetectReceiveInterval = value;
            }
        }

        public static int ReceiveTimeout
        {
            get => _receiveTimeout;
            set
            {
                if (value <= 0)
                    throw new Exception();

                _receiveTimeout = value;
            }
        }

        public static int SendTimeout
        {
            get => _sendTimeout;
            set
            {
                if (value <= 0)
                    throw new Exception();

                _sendTimeout = value;
            }
        }

        public static int WriteBufferSize
        {
            get => _writeBufferSize;
            set
            {
                if (value < 200)
                    throw new Exception();

                _writeBufferSize = value;
            }
        }

        public static int ReadBufferSize
        {
            get => _readBufferSize;
            set
            {
                if (value < 200)
                    throw new Exception();

                _readBufferSize = value;
            }
        }

        public static int ClientCacheSize
        {
            get => _clientCacheSize;
            set
            {
                if (value < 0)
                    throw new Exception();

                _clientCacheSize = value;
            }
        }

        public static int ReconnectInterval
        {
            get => _reconnectInterval;
            set
            {
                if (value <= 0)
                    throw new Exception();

                _reconnectInterval = value;
            }
        }

        public static int ServerListenBacklog
        {
            get => _serverListenBacklog;
            set
            {
                if (value <= 0)
                    throw new Exception();

                _serverListenBacklog = value;
            }
        }

        public static TcpSendMode TcpRequestSendMode { get; set; } = TcpSendMode.Default;
    }

    public enum TcpSendMode
    {
        Default = 0,
        Sync = 0,
        Async = 1
    }
}
