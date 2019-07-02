using System.Net;

namespace Socean.Rpc.Core.Server
{
    public sealed class KeepAliveRpcServer: IServer, IKeepAlive
    {
        public KeepAliveRpcServer() 
        {
            _rpcServer = new RpcServer();
        }

        public IPAddress ServerIP { get; private set; }
        public int ServerPort { get; private set; }
        
        private volatile bool _isManualClosed = false;
        private volatile bool _autoReconnect = false;
        private RpcServer _rpcServer;

        public IMessageProcessor MessageProcessor { get; set; }

        public bool AutoReconnect
        {
            get { return _autoReconnect; }
            set
            {
                if (value)
                {
                    _autoReconnect = value;
                    KeepAliveMonitor.Instance.Attach(this);
                }
                else
                {
                    _autoReconnect = value;
                    KeepAliveMonitor.Instance.Detach(this);
                }
            }
        }

        public void Bind(IPAddress ip, int port)
        {
            ServerIP = ip;
            ServerPort = port;
        }

        public void Start()
        {
            _rpcServer.Bind(ServerIP, ServerPort);
            _rpcServer.MessageProcessor = MessageProcessor;
            _rpcServer.Start();
        }

        public void CheckConnection()
        {
            if (_autoReconnect == false)
                return;

            if (_isManualClosed)
                return;

            if (_rpcServer.ServerState != -1)
                return;

            try
            {
                _rpcServer.Close();
            }
            catch
            {
                
            }

            _rpcServer = new RpcServer();
            _rpcServer.Bind(ServerIP, ServerPort);
            _rpcServer.MessageProcessor = MessageProcessor; 
            _rpcServer.Start();
        }

        public void Close()
        {
            _isManualClosed = true;

            try
            {
                _rpcServer.Close();
            }
            catch 
            {
             
            }
        }

        public void Dispose()
        {
            Close();
        }

        public int GetClientCount()
        {
            return _rpcServer.GetClientCount();
        }
    }
}
