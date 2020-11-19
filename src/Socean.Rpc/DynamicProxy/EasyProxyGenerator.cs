using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;

namespace Socean.Rpc.DynamicProxy
{
    public static class EasyProxyGenerator<T> where T : class
    {
        private static volatile Type ProxyType;
        private static readonly Type InterfaceType;
        private static readonly ModuleBuilder ModuleBuilder;
        private static readonly AssemblyBuilder AssemblyBuilder;
        private static readonly IInterceptor DefaultInterceptor;

        static EasyProxyGenerator()
        {
            AssemblyName assemblyName = new AssemblyName("Socean.Rpc.EasyProxy");
            AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            ModuleBuilder = AssemblyBuilder.DefineDynamicModule(assemblyName.Name);
            DefaultInterceptor = new EasyProxyInterceptor();
            InterfaceType = typeof(T);
        }

        public static T Create(IPAddress ip, int port, IBinarySerializer serializer,string extention = null)
        {
            if (ProxyType == null)
            {
                if (serializer == null)
                    throw new ArgumentNullException(nameof(serializer));

                if (!InterfaceType.IsInterface)
                    throw new ArgumentException(string.Format("[{0}] is not valid for {1}.Create,only interface type is valid", InterfaceType, typeof(EasyProxyGenerator<T>).Name));

                var parentInterfaceArray = InterfaceType.GetInterfaces();
                if (parentInterfaceArray != null && parentInterfaceArray.Length > 0)
                    throw new NotSupportedException(string.Format("[{0}] is not valid for {1}.Create", InterfaceType, typeof(EasyProxyGenerator<T>).Name));

                var proxyAttribute = (RpcProxyAttribute)InterfaceType.GetCustomAttributes(typeof(RpcProxyAttribute), false).FirstOrDefault();
                if (proxyAttribute == null)
                    throw new ArgumentException(string.Format("{0} is not defined for [{1}]", typeof(RpcProxyAttribute).Name, InterfaceType));

                lock (DefaultInterceptor)
                {
                    if (ProxyType == null)
                    {
                        ProxyType = CreateProxyType();
                    }
                }
            }

            var proxy = (ProxyBase)ObjectFactory.CreateInstance(ProxyType, new object[1] { DefaultInterceptor });
            proxy.__IP = ip;
            proxy.__Port = port;
            proxy.__InterfaceType = InterfaceType;
            proxy.__Extention = extention;
            proxy.__BinarySerializer = serializer;

            return proxy as T;
        }

        private static Type CreateProxyType()
        {
            var interfaceType = InterfaceType;
            var typeName = string.Format("{0}_{1}_Imp_{2}", typeof(EasyProxyGenerator<T>).Name, interfaceType.Name, interfaceType.GetHashCode());

            TypeBuilder typeBuilder = ModuleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed, typeof(ProxyBase));
            typeBuilder.AddInterfaceImplementation(interfaceType);

            MethodInfo interceptMethodInfo = typeof(IInterceptor).GetMethod("Proceed", BindingFlags.Instance | BindingFlags.Public);
            FieldBuilder fieldBuilder = DynamicProxyHelper.DefineField(typeBuilder, "interceptor", typeof(IInterceptor), FieldAttributes.Private);

            DynamicProxyHelper.DefineConstructor(typeBuilder, fieldBuilder);
            DynamicProxyHelper.DefineMethods(typeBuilder, fieldBuilder, interfaceType, interceptMethodInfo);

            return typeBuilder.CreateTypeInfo().AsType();
        }
    }
}
