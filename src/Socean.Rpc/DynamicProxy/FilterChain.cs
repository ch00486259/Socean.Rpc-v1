using Socean.Rpc.Core;
using Socean.Rpc.Core.Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Socean.Rpc.DynamicProxy
{
    public class FilterChain
    {
        private readonly LinkedList<IServiceFilter> _filterLinkedList = new LinkedList<IServiceFilter>();

        public void AddFirst(IServiceFilter filter)
        {
            if (filter == null)
                return;

            _filterLinkedList.AddFirst(filter);
        }

        public void AddLast(IServiceFilter filter)
        {
            if (filter == null)
                return;

            _filterLinkedList.AddLast(filter);
        }

        public void DoNext(ServiceContext context)
        {
            var node = context.CurrentFilterNode.Next;
            if (node == null)
                return;

            context.CurrentFilterNode = node;
            node.Value.Do(context, this);
        }

        internal ResponseBase Do(FrameData frameData)
        {
            var context = new ServiceContext(frameData);
            context.CurrentFilterNode = _filterLinkedList.First;

            _filterLinkedList.First.Value.Do(context, this);

            return context.Response.FlushFinal();
        }
    }

    public interface IServiceFilter
    {
        void Do(ServiceContext context, FilterChain filterChain);
    }

    internal class FinalProcessFilter : IServiceFilter
    {
        private readonly ConcurrentDictionary<string, Invocation> _invocationDictionary = new ConcurrentDictionary<string, Invocation>();
        private readonly ConcurrentDictionary<string, string> _assemblyTagDictionary = new ConcurrentDictionary<string, string>();

        internal void RegisterServices(Assembly assembly, IBinarySerializer serializer)
        {
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));

            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            if (!_assemblyTagDictionary.TryAdd(assembly.FullName, assembly.Location))
                return;

            var serviceTypeList = ResolveServiceTypeList(assembly) ?? new List<Tuple<Type, RpcServiceAttribute>>();

            RegisterServices(serviceTypeList, serializer);
        }

        private List<Tuple<Type, RpcServiceAttribute>> ResolveServiceTypeList(Assembly assembly)
        {
            var rpcServiceAttributeType = typeof(RpcServiceAttribute);

            return assembly.GetTypes()
                .Select(type => new Tuple<Type, RpcServiceAttribute>(type, (RpcServiceAttribute)type.GetCustomAttributes(rpcServiceAttributeType, false).FirstOrDefault()))
                .Where(tuple => tuple.Item2 != null)
                .ToList();
        }

        private void RegisterServices(List<Tuple<Type, RpcServiceAttribute>> serviceTypeList, IBinarySerializer serializer)
        {
            foreach (var tuple in serviceTypeList)
            {
                var serviceType = tuple.Item1;
                var serviceAttribute = tuple.Item2;

                try
                {
                    var service = ObjectFactory.CreateInstance(serviceType);

                    var serviceName = serviceAttribute.ServiceName;
                    if (string.IsNullOrEmpty(serviceName))
                        serviceName = serviceType.Name;

                    var methodInfoArray = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var methodInfo in methodInfoArray)
                    {
                        var actionAttribute = (RpcServiceActionAttribute)methodInfo.GetCustomAttributes(typeof(RpcServiceActionAttribute), false).FirstOrDefault();
                        var actionName = actionAttribute?.ActionName;
                        if (string.IsNullOrEmpty(actionName))
                        {
                            actionName = methodInfo.Name;
                        }

                        RegisterService(serviceName, actionName, service, serviceType, methodInfo, serializer);
                    }
                }
                catch (Exception ex)
                {
                    LogAgent.Warn(string.Format("RegisterService failed,type:{0}", serviceType.Name), ex);
                }
            }
        }

        private void RegisterService(string serviceName, string actionName, object service, Type serviceType, MethodInfo methodInfo, IBinarySerializer serializer)
        {
            var typeArray = DynamicProxyHelper.GetParameterTypes(methodInfo.GetParameters());
            if (typeArray.Length > 10)
                throw new ArgumentException(string.Format("RegisterService error,service:{0},action:{1}, parameters count is not valid, max method parameter count is {2} ", serviceName, actionName, 10));

            var parameterTupleType = CustomTuple.CreateType(typeArray);
            var title = DynamicProxyHelper.FormatTitle(serviceName, actionName, typeArray, methodInfo.ReturnType);

            var invocation = new Invocation(title, serviceType, methodInfo, parameterTupleType, serializer);
            _invocationDictionary.TryAdd(invocation.Key, invocation);

            LogAgent.Info(string.Format("RegisterService -> {0}", title));
        }

        public void Do(ServiceContext context, FilterChain filterChain)
        {
            var title = context.Request.Title;
            var contentBytes = context.Request.ContentBytes;

            _invocationDictionary.TryGetValue(title, out var _invocation);
            if (_invocation == null)
            {
                context.Response.WriteError((byte)ResponseCode.SERVICE_NOT_FOUND);
                return;
            }

            try
            {
                var responseBytes = _invocation.Proceed(contentBytes);
                context.Response.WriteBytes(responseBytes);
            }
            catch
            {
                context.Response.WriteError((byte)ResponseCode.SERVER_INTERNAL_ERROR);
            }
        }
    }

    public class TransportEncryptFilter : IServiceFilter
    {
        private byte[] _passwordBytes = new byte[32];

        public TransportEncryptFilter(string password)
        {
            var bytes = Encoding.ASCII.GetBytes(password ?? string.Empty);
            var copyLength = Math.Min(bytes.Length, _passwordBytes.Length);
            if (copyLength > 0)
                Buffer.BlockCopy(bytes, 0, _passwordBytes, 0, copyLength);
        }

        public void Do(ServiceContext context, FilterChain filterChain)
        {
            context.Request.DecryptMessage(_passwordBytes);

            try
            {
                filterChain.DoNext(context);
            }
            catch
            {
                context.Response.WriteError((byte)ResponseCode.FILTER_INTERNAL_ERROR);
            }

            context.Response.EncryptMessage(_passwordBytes);
        }
    }
}
