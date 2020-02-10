using Socean.Rpc.Core;
using Socean.Rpc.Core.Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Socean.Rpc.DynamicProxy
{
    public abstract class EasyProxyMessageProcessor : IMessageProcessor
    {
        private readonly ConcurrentDictionary<string, Invocation> _invocationDictionary = new ConcurrentDictionary<string, Invocation>();
        private readonly ConcurrentDictionary<string, string> _assemblyTagDictionary = new ConcurrentDictionary<string, string>();

        public abstract void Init();

        protected void RegisterServices(Assembly assembly, IRpcSerializer rpcSerializer)
        {
            if (rpcSerializer == null)
                throw new ArgumentNullException("rpcSerializer");

            if (assembly == null)
                throw new ArgumentNullException("assembly");

            if (!_assemblyTagDictionary.TryAdd(assembly.FullName, assembly.Location))
                return;

            var serviceTypeList = ResolveServiceTypeList(assembly) ?? new List<Tuple<Type, RpcServiceAttribute>>();

            RegisterServices(serviceTypeList, rpcSerializer);
        }

        private List<Tuple<Type, RpcServiceAttribute>> ResolveServiceTypeList(Assembly assembly)
        {
            var rpcServiceAttributeType = typeof(RpcServiceAttribute);

            return assembly.GetTypes()
                .Select(type => new Tuple<Type, RpcServiceAttribute>(type, (RpcServiceAttribute)type.GetCustomAttributes(rpcServiceAttributeType, false).FirstOrDefault()))
                .Where(tuple => tuple.Item2 != null)
                .ToList();
        }

        private void RegisterServices(List<Tuple<Type, RpcServiceAttribute>> serviceTypeList, IRpcSerializer rpcSerializer)
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

                        RegisterService(serviceName, actionName, service, serviceType, methodInfo, rpcSerializer);
                    }
                }
                catch (Exception ex)
                {
                    LogAgent.Warn(string.Format("RegisterService failed,type:{0}", serviceType.Name), ex);
                }
            }
        }

        private void RegisterService(string serviceName, string actionName, object service, Type serviceType, MethodInfo methodInfo, IRpcSerializer rpcSerializer)
        {
            var typeArray = DynamicProxyHelper.GetParameterTypes(methodInfo.GetParameters());
            if (typeArray.Length > 10)
                throw new ArgumentException(string.Format("RegisterService error,service:{0},action:{1}, parameters count is not valid, max method parameter count is {2} ", serviceName, actionName, 10));

            var parameterTupleType = CustomTuple.CreateType(typeArray);
            var title = DynamicProxyHelper.FormatTitle(serviceName, actionName, typeArray, methodInfo.ReturnType);

            var invocation = new Invocation(title, serviceType, methodInfo, parameterTupleType, rpcSerializer);
            _invocationDictionary.TryAdd(invocation.Key, invocation);

            LogAgent.Info(string.Format("RegisterService -> {0}", title));
        }

        public Task<ResponseBase> Process(FrameData frameData)
        {
            var title = frameData.ReadTitleString();
            var content = frameData.ReadContentString();

            if (string.IsNullOrEmpty(title))
                return Task.FromResult<ResponseBase>(new ErrorResponse((byte)ResponseCode.SERVICE_TITLE_ERROR));

            _invocationDictionary.TryGetValue(title, out var _invocation);
            if (_invocation == null)
                return Task.FromResult<ResponseBase>(new ErrorResponse((byte)ResponseCode.SERVICE_NOT_FOUND));

            var responseString = _invocation.Proceed(content);
            return Task.FromResult<ResponseBase>(new StringResponse(responseString));
        }
    }
}