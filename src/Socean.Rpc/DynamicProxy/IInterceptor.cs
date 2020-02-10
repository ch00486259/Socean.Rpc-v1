using System;

namespace Socean.Rpc.DynamicProxy
{
    public interface IInterceptor
    {
        object Proceed(ProxyBase target, string title, Type parameterTupleType, object[] parameterArray, Type returnType);
    }
}
