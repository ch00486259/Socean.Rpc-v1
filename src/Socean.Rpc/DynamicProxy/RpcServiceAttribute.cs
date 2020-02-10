using System;

namespace Socean.Rpc.DynamicProxy
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RpcServiceAttribute : Attribute
    {
        public RpcServiceAttribute()
        {

        }

        public string ServiceName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RpcServiceActionAttribute : Attribute
    {
        public RpcServiceActionAttribute()
        {

        }

        public string ActionName { get; set; }
    }


    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class RpcProxyAttribute : Attribute
    {
        public RpcProxyAttribute()
        {

        }

        public string ServiceName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RpcProxyActionAttribute : Attribute
    {
        public RpcProxyActionAttribute()
        {

        }

        public string ActionName { get; set; }
    }
}
