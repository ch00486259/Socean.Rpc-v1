using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Socean.Rpc.Core
{
    internal sealed class KeepAliveMonitor
    {
        private KeepAliveMonitor()
        {

        }

        internal static KeepAliveMonitor Instance
        {
            get { return _instance; }
        }

        private static readonly KeepAliveMonitor _instance = new KeepAliveMonitor();

        private readonly ConcurrentDictionary<IKeepAlive, int> _observerDictionary = new ConcurrentDictionary<IKeepAlive, int>();
        private volatile int _state = 0;
        private Thread _thread;

        private void Init()
        {
            if (_state == 1)
                return;

            var oldValue = Interlocked.Exchange(ref _state, 1);
            if(oldValue == 1)
                return;

            _thread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(NetworkSettings.ReconnectInterval);

                    Instance.CheckConnection();
                }
            });
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void CheckConnection()
        {
            var observerList = _observerDictionary.Keys.ToList();
            foreach (var observer in observerList)
            {
                try
                {
                    observer.CheckConnection();
                }
                catch (Exception ex)
                {
                    LogAgent.Error(ex.Message, ex);
                }
            }
        }

        internal void Attach(IKeepAlive observer)
        {
            _observerDictionary.TryAdd(observer, 0);

            if (_state == 0)
                Init();
        }

        internal void Detach(IKeepAlive observer)
        {
            _observerDictionary.TryRemove(observer, out var _);
        }
    }
}
