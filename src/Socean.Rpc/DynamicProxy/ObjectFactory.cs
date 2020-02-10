using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace Socean.Rpc.DynamicProxy
{
    internal static class ObjectFactory
    {
        private static readonly ConcurrentDictionary<Type, Func<object>> _noParamCreateFuncDictionary = new ConcurrentDictionary<Type, Func<object>>();
        private static readonly ConcurrentDictionary<Type, Func<object[], object>> _createFuncDictionary = new ConcurrentDictionary<Type, Func<object[], object>>();

        private static Func<object> GetCreateFunc(Type type)
        {
            var ctor = type.GetConstructor(Type.EmptyTypes);
            DynamicMethod method = new DynamicMethod(String.Empty, type, null);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);
            return method.CreateDelegate(typeof(Func<object>)) as Func<object>;
        }

        public static object CreateInstance(Type type)
        {
            _noParamCreateFuncDictionary.TryGetValue(type, out var _createFunc);
            if (_createFunc != null)
                return _createFunc();

            _noParamCreateFuncDictionary[type] = GetCreateFunc(type);
            _noParamCreateFuncDictionary.TryGetValue(type, out var _createFunc2);
            if (_createFunc2 != null)
                return _createFunc2();

            throw new Exception();
        }

        public static T CreateInstance<T>()
        {
            return (T)CreateInstance(typeof(T));
        }

        public static object CreateInstance(Type type, object[] parameterArray)
        {
            _createFuncDictionary.TryGetValue(type, out var _createFunc);
            if (_createFunc != null)
                return _createFunc(parameterArray);

            var typeList = DynamicProxyHelper.GetParameterTypes(parameterArray);

            _createFuncDictionary[type] = CreateHandler(type, typeList);
            _createFuncDictionary.TryGetValue(type, out var _createFunc2);
            if (_createFunc2 != null)
                return _createFunc2(parameterArray);

            throw new Exception();
        }

        public static Func<object[], object> CreateHandler(Type type, Type[] parameterTypeArray)
        {
            DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, typeof(object), new Type[] { typeof(object[]) }, type.Module);
            ILGenerator il = dynamicMethod.GetILGenerator();
            ConstructorInfo constructorInfo = type.GetConstructor(parameterTypeArray);

            if (constructorInfo == null)
                throw new MissingMethodException("The constructor for the corresponding parameter was not found");

            for (int i = 0; i < parameterTypeArray.Length; i++)
            {
                var parameterType = parameterTypeArray[i];

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);
                if (parameterType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, parameterType);
                else
                    il.Emit(OpCodes.Castclass, parameterType);
            }

            il.Emit(OpCodes.Newobj, constructorInfo);
            il.Emit(OpCodes.Ret);

            return (Func<object[], object>)dynamicMethod.CreateDelegate(typeof(Func<object[], object>));
        }     
    }
}
