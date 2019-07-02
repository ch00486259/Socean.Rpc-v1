using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Socean.Rpc.Core.Client
{
    public sealed class ShortConnectionRpcClientFactory
    {
        public static IClient Create(IPAddress ip, int port)
        {
            return new ShortConnectionRpcClient(ip, port);
        }
    }
}
