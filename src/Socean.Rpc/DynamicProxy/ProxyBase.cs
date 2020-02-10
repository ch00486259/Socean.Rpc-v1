using System;

namespace Socean.Rpc.DynamicProxy
{
    public abstract class ProxyBase
    {
        public string __IP;
        public int __Port;
        public Type __InterfaceType;
        public string __Extention;
        public IRpcSerializer __RpcSerializer;
    }
}
