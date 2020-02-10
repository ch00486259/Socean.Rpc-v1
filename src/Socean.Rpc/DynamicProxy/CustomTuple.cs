using System;

namespace Socean.Rpc.DynamicProxy
{
    public interface ICustomTuple
    {
        object[] ToObjectArray();

        void Fill(object[] parameterArray);
    }

    public class CustomTuple : ICustomTuple
    {
        public object[] ToObjectArray()
        {
            return new object[0];
        }

        public static Type CreateType(Type[] typeArray)
        {
            if (typeArray == null || typeArray.Length == 0)
                return typeof(CustomTuple);

            if (typeArray.Length > 10)
                throw new ArgumentException("error interface definition ,max method parameter count is 10");

            Type typleType = Type.GetType("Socean.Rpc.DynamicProxy.CustomTuple`" + typeArray.Length);
            return typleType.MakeGenericType(typeArray);
        }

        public void Fill(object[] parameterArray)
        {

        }
    }

    public class CustomTuple<T1> : ICustomTuple
    {
        public T1 Item1 { get; set; }

        public object[] ToObjectArray()
        {
            return new object[1] { Item1 };
        }

        public void Fill(object[] parameterArray)
        {
            Item1 = (T1)parameterArray[0];
        }
    }

    public class CustomTuple<T1, T2> : ICustomTuple
    {
        public T1 Item1 { get; set; }

        public T2 Item2 { get; set; }

        public object[] ToObjectArray()
        {
            return new object[2] { Item1, Item2 };
        }

        public void Fill(object[] parameterArray)
        {
            Item1 = (T1)parameterArray[0];
            Item2 = (T2)parameterArray[1];
        }
    }

    public class CustomTuple<T1, T2, T3> : ICustomTuple
    {
        public T1 Item1 { get; set; }

        public T2 Item2 { get; set; }

        public T3 Item3 { get; set; }

        public object[] ToObjectArray()
        {
            return new object[3] { Item1, Item2, Item3 };
        }

        public void Fill(object[] parameterArray)
        {
            Item1 = (T1)parameterArray[0];
            Item2 = (T2)parameterArray[1];
            Item3 = (T3)parameterArray[2];
        }
    }

    public class CustomTuple<T1, T2, T3, T4> : ICustomTuple
    {
        public T1 Item1 { get; set; }

        public T2 Item2 { get; set; }

        public T3 Item3 { get; set; }

        public T4 Item4 { get; set; }

        public object[] ToObjectArray()
        {
            return new object[4] { Item1, Item2, Item3, Item4 };
        }

        public void Fill(object[] parameterArray)
        {
            Item1 = (T1)parameterArray[0];
            Item2 = (T2)parameterArray[1];
            Item3 = (T3)parameterArray[2];
            Item4 = (T4)parameterArray[3];
        }
    }

    public class CustomTuple<T1, T2, T3, T4, T5> : ICustomTuple
    {
        public T1 Item1 { get; set; }

        public T2 Item2 { get; set; }

        public T3 Item3 { get; set; }

        public T4 Item4 { get; set; }

        public T5 Item5 { get; set; }

        public object[] ToObjectArray()
        {
            return new object[5] { Item1, Item2, Item3, Item4, Item5 };
        }

        public void Fill(object[] parameterArray)
        {
            Item1 = (T1)parameterArray[0];
            Item2 = (T2)parameterArray[1];
            Item3 = (T3)parameterArray[2];
            Item4 = (T4)parameterArray[3];
            Item5 = (T5)parameterArray[4];
        }
    }

    public class CustomTuple<T1, T2, T3, T4, T5, T6> : ICustomTuple
    {
        public T1 Item1 { get; set; }

        public T2 Item2 { get; set; }

        public T3 Item3 { get; set; }

        public T4 Item4 { get; set; }

        public T5 Item5 { get; set; }

        public T6 Item6 { get; set; }

        public object[] ToObjectArray()
        {
            return new object[6] { Item1, Item2, Item3, Item4, Item5, Item6 };
        }

        public void Fill(object[] parameterArray)
        {
            Item1 = (T1)parameterArray[0];
            Item2 = (T2)parameterArray[1];
            Item3 = (T3)parameterArray[2];
            Item4 = (T4)parameterArray[3];
            Item5 = (T5)parameterArray[4];
            Item6 = (T6)parameterArray[5];
        }
    }

    public class CustomTuple<T1, T2, T3, T4, T5, T6, T7> : ICustomTuple
    {
        public T1 Item1 { get; set; }

        public T2 Item2 { get; set; }

        public T3 Item3 { get; set; }

        public T4 Item4 { get; set; }

        public T5 Item5 { get; set; }

        public T6 Item6 { get; set; }

        public T7 Item7 { get; set; }

        public object[] ToObjectArray()
        {
            return new object[7] { Item1, Item2, Item3, Item4, Item5, Item6, Item7 };
        }

        public void Fill(object[] parameterArray)
        {
            Item1 = (T1)parameterArray[0];
            Item2 = (T2)parameterArray[1];
            Item3 = (T3)parameterArray[2];
            Item4 = (T4)parameterArray[3];
            Item5 = (T5)parameterArray[4];
            Item6 = (T6)parameterArray[5];
            Item7 = (T7)parameterArray[6];
        }
    }

    public class CustomTuple<T1, T2, T3, T4, T5, T6, T7, T8> : ICustomTuple
    {
        public T1 Item1 { get; set; }

        public T2 Item2 { get; set; }

        public T3 Item3 { get; set; }

        public T4 Item4 { get; set; }

        public T5 Item5 { get; set; }

        public T6 Item6 { get; set; }

        public T7 Item7 { get; set; }

        public T8 Item8 { get; set; }

        public object[] ToObjectArray()
        {
            return new object[8] { Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8 };
        }

        public void Fill(object[] parameterArray)
        {
            Item1 = (T1)parameterArray[0];
            Item2 = (T2)parameterArray[1];
            Item3 = (T3)parameterArray[2];
            Item4 = (T4)parameterArray[3];
            Item5 = (T5)parameterArray[4];
            Item6 = (T6)parameterArray[5];
            Item7 = (T7)parameterArray[6];
            Item8 = (T8)parameterArray[7];
        }
    }

    public class CustomTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ICustomTuple
    {
        public T1 Item1 { get; set; }

        public T2 Item2 { get; set; }

        public T3 Item3 { get; set; }

        public T4 Item4 { get; set; }

        public T5 Item5 { get; set; }

        public T6 Item6 { get; set; }

        public T7 Item7 { get; set; }

        public T8 Item8 { get; set; }

        public T9 Item9 { get; set; }

        public object[] ToObjectArray()
        {
            return new object[9] { Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9 };
        }

        public void Fill(object[] parameterArray)
        {
            Item1 = (T1)parameterArray[0];
            Item2 = (T2)parameterArray[1];
            Item3 = (T3)parameterArray[2];
            Item4 = (T4)parameterArray[3];
            Item5 = (T5)parameterArray[4];
            Item6 = (T6)parameterArray[5];
            Item7 = (T7)parameterArray[6];
            Item8 = (T8)parameterArray[7];

            Item9 = (T9)parameterArray[8];
        }
    }

    public class CustomTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ICustomTuple
    {
        public T1 Item1 { get; set; }

        public T2 Item2 { get; set; }

        public T3 Item3 { get; set; }

        public T4 Item4 { get; set; }

        public T5 Item5 { get; set; }

        public T6 Item6 { get; set; }

        public T7 Item7 { get; set; }

        public T8 Item8 { get; set; }

        public T9 Item9 { get; set; }

        public T10 Item10 { get; set; }


        public object[] ToObjectArray()
        {
            return new object[10] { Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9, Item10 };
        }

        public void Fill(object[] parameterArray)
        {
            Item1 = (T1)parameterArray[0];
            Item2 = (T2)parameterArray[1];
            Item3 = (T3)parameterArray[2];
            Item4 = (T4)parameterArray[3];
            Item5 = (T5)parameterArray[4];
            Item6 = (T6)parameterArray[5];
            Item7 = (T7)parameterArray[6];
            Item8 = (T8)parameterArray[7];

            Item9 = (T9)parameterArray[8];
            Item10 = (T10)parameterArray[9];
        }
    }
}
