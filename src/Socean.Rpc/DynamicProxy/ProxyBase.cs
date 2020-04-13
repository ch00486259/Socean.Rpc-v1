using System;
using System.Net;

namespace Socean.Rpc.DynamicProxy
{
    public abstract class ProxyBase
    {
        public IPAddress __IP;
        public int __Port;
        public Type __InterfaceType;
        public string __Extention;
        public IBinarySerializer __BinarySerializer;
    }
}
