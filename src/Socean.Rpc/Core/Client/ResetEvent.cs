using System;
using System.Threading;

namespace Socean.Rpc.Core.Client
{
    internal class ResetEvent
    {
        private object _key = new object();

        private bool _setFlag = false;

        public void Set()
        {
            lock (_key)
            {
                _setFlag = true;

                Monitor.Pulse(_key);
            }
        }

        public void WaitOne(int timeoutMilliseconds)
        {
            lock (_key)
            {
                if (_setFlag == true)
                {
                    _setFlag = false;
                    return;
                }

                var isTimeout = Monitor.Wait(_key, timeoutMilliseconds);
                if (isTimeout == false)
                {
                    _setFlag = false;
                    return;
                }
                _setFlag = false;
            }
        }
    }
}
