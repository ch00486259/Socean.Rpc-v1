using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Socean.Rpc.DynamicProxy
{
    internal delegate object FastInvokeHandler(object target, object[] parameters);

    internal static class DynamicProxyHelper
    {
        public static FastInvokeHandler CreateFastInvokeHandler(MethodInfo methodInfo)
        {
            DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, typeof(object), new Type[] { typeof(object), typeof(object[]) }, methodInfo.DeclaringType.Module);
            ILGenerator il = dynamicMethod.GetILGenerator();
            ParameterInfo[] parameterArray = methodInfo.GetParameters();
            Type[] parameterTypeArray = new Type[parameterArray.Length];

            for (int i = 0; i < parameterTypeArray.Length; i++)
            {
                parameterTypeArray[i] = parameterArray[i].ParameterType;
            }

            LocalBuilder[] localBuilderArray = new LocalBuilder[parameterTypeArray.Length];
            for (int i = 0; i < parameterTypeArray.Length; i++)
            {
                localBuilderArray[i] = il.DeclareLocal(parameterTypeArray[i], true);
            }

            for (int i = 0; i < parameterTypeArray.Length; i++)
            {
                var parameterType = parameterTypeArray[i];

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);

                if (parameterType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, parameterType);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, parameterType);
                }

                il.Emit(OpCodes.Stloc, localBuilderArray[i]);
            }

            if (!methodInfo.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }

            for (int i = 0; i < parameterTypeArray.Length; i++)
            {
                il.Emit(OpCodes.Ldloc, localBuilderArray[i]);
            }

            if (methodInfo.IsStatic)
                il.EmitCall(OpCodes.Call, methodInfo, null);
            else
                il.EmitCall(OpCodes.Callvirt, methodInfo, null);

            if (methodInfo.ReturnType == typeof(void))
            {
                il.Emit(OpCodes.Ldnull);
            }
            else
            {
                if (methodInfo.ReturnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, methodInfo.ReturnType);
                }
            }

            il.Emit(OpCodes.Ret);

            var handler = (FastInvokeHandler)dynamicMethod.CreateDelegate(typeof(FastInvokeHandler));
            return handler;
        }

        private const MethodAttributes DefaultMethodAttributes = MethodAttributes.Public | MethodAttributes.NewSlot |
            MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig;

        public static void DefineConstructor(TypeBuilder typeBuilder, FieldBuilder fieldBuilder)
        {
            Type[] parameterTypeArray = new Type[] { typeof(IInterceptor) };
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypeArray);
            ILGenerator il = constructorBuilder.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, fieldBuilder);
            il.Emit(OpCodes.Ret);
        }

        public static FieldBuilder DefineField(TypeBuilder typeBuilder, string fieldName, Type fieldType, FieldAttributes fieldAttributes)
        {
            return typeBuilder.DefineField(fieldName, fieldType, fieldAttributes);
        }
                    
        public static void DefineMethods(TypeBuilder typeBuilder, FieldBuilder fieldBuilder, Type interfaceType, MethodInfo interceptMethodInfo)
        {
            var methodArray = interfaceType.GetMethods();
            foreach (MethodInfo interfaceMethodInfo in methodArray)
            {
                var title = GenerateTitle(interfaceType,interfaceMethodInfo);

                var parameterArray = interfaceMethodInfo.GetParameters();
                var parameterTypeArray = GetParameterTypes(parameterArray);
                var parameterTupleType = CustomTuple.CreateType(parameterTypeArray);

                DefineMethod(interfaceMethodInfo, interceptMethodInfo, parameterTupleType, typeBuilder, fieldBuilder, title);
            }
        }

        private static string GenerateTitle(Type interfaceType, MethodInfo interfaceMethodInfo)
        {
            var interfaceAttribute = (RpcProxyAttribute)interfaceType.GetCustomAttributes(typeof(RpcProxyAttribute), false).FirstOrDefault();
            var serviceName = interfaceAttribute?.ServiceName;
            if (string.IsNullOrEmpty(serviceName))
                serviceName = interfaceType.Name;

            var parameterArray = interfaceMethodInfo.GetParameters();
            var paremeterTypeArray = GetParameterTypes(parameterArray);
            var title = FormatTitle(serviceName, interfaceMethodInfo.Name, paremeterTypeArray, interfaceMethodInfo.ReturnType);

            return title;
        }

        private static MethodBuilder DefineMethod(MethodInfo interfaceMethodInfo, MethodInfo interceptMethodInfo,Type parameterTupleType, TypeBuilder typeBuilder, FieldBuilder fieldBuilder, string title)
        {
            ParameterInfo[] parameterArray = interfaceMethodInfo.GetParameters();

            MethodBuilder methodBuilder = typeBuilder.DefineMethod(interfaceMethodInfo.Name, DefaultMethodAttributes, interfaceMethodInfo.ReturnType, DynamicProxyHelper.GetParameterTypes(parameterArray));
            ILGenerator il = methodBuilder.GetILGenerator();

            il.DeclareLocal(typeof(object[]));

            if (interfaceMethodInfo.ReturnType != typeof(void))
            {
                il.DeclareLocal(interfaceMethodInfo.ReturnType);
            }

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldc_I4, parameterArray.Length);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc_0);

            for (int i = 0; i < parameterArray.Length; i++)
            {
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldarg, 1 + i);
                il.Emit(OpCodes.Box, parameterArray[i].ParameterType);
                il.Emit(OpCodes.Stelem_Ref);
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fieldBuilder);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, title);

            il.Emit(OpCodes.Ldtoken, parameterTupleType);

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldtoken, interfaceMethodInfo.ReturnType);
            il.Emit(OpCodes.Call, interceptMethodInfo);

            if (interfaceMethodInfo.ReturnType == typeof(void))
            {
                il.Emit(OpCodes.Pop);
            }
            else
            {
                il.Emit(OpCodes.Unbox_Any, interfaceMethodInfo.ReturnType);
                il.Emit(OpCodes.Stloc_1);
                il.Emit(OpCodes.Ldloc_1);
            }

            il.Emit(OpCodes.Ret);

            return methodBuilder;
        }
  
        public static string FormatTitle(string serviceName, string actionName,Type[] parameterTypeArray,Type returnType)
        {
            var parameterString = string.Join(",", parameterTypeArray.Select(type => type.Name));
            return string.Format("[{0}].[{1}]({2})", serviceName, actionName, parameterString);
        }

        public static Type[] GetParameterTypes(ParameterInfo[] parameterInfoArray)
        {
            Type[] buffer = new Type[parameterInfoArray.Length];
            for (int i = 0; i < parameterInfoArray.Length; i++)
            {
                buffer[i] = parameterInfoArray[i].ParameterType;
            }
            return buffer;
        }

        public static Type[] GetParameterTypes(object[] parameterArray)
        {
            Type[] buffer = new Type[parameterArray.Length];
            for (int i = 0; i < parameterArray.Length; i++)
            {
                buffer[i] = parameterArray[i].GetType();
            }
            return buffer;
        }
    }
}
