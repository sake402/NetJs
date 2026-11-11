using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System
{
    [NetJs.ForcePartial(typeof(ValueType))]
    [NetJs.Boot]
    [NetJs.OutputOrder(int.MinValue + 2)]
    public abstract partial class ValueType_Partial
    {
        [NetJs.MemberReplace]
        private static int InternalGetHashCode(object o, out object[]? fields)
        {
            fields = null;
            return RuntimeHelpers.GetHashCode(o);
        }

        [NetJs.MemberReplace]
        private static bool InternalEquals(object o1, object o2, out object[] fields)
        {
            fields = null;
            var type1 = o1.GetType();
            var type2 = o2.GetType();
            if (type1 != type2)
                return false;
            var fieldInfos = type1.GetFields();
            var propertyInfos = type1.GetProperties();
            var mfields = NetJs.Script.NewArray<object>();
            unchecked
            {
                for (int i = 0; i < fieldInfos.Length; i++)
                {
                    if (fieldInfos[i].Name.NativeStartsWith("$")) //internal object fields
                        continue;
                    var f1 = fieldInfos[i].GetValue(o1);
                    var f2 = fieldInfos[i].GetValue(o2);
                    mfields.push(f1);
                    mfields.push(f2);
                }
                for (int i = 0; i < propertyInfos.Length; i++)
                {
                    if (propertyInfos[i].Name.NativeStartsWith("$")) //internal object fields
                        continue;
                    if (propertyInfos[i].CanRead)
                    {
                        var f1 = propertyInfos[i].GetValue(o1);
                        var f2 = propertyInfos[i].GetValue(o2);
                        mfields.push(f1);
                        mfields.push(f2);
                    }
                }
            }
            fields = mfields;
            return true;
        }

    }

    //public static class ValueTupleExtensions
    //{
    //    public static bool Equals<T1>(this ValueTuple<T1> value, ValueTuple<T1> other)
    //    {
    //        return EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1);
    //    }

    //    public static bool NotEquals<T1>(this ValueTuple<T1> value, ValueTuple<T1> other)
    //    {
    //        return !EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1);
    //    }

    //    public static bool Equals<T1, T2>(this ValueTuple<T1, T2> value, ValueTuple<T1, T2> other)
    //    {
    //        return EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1) &&
    //               EqualityComparer<T2>.Default.Equals(value.Item2, other.Item2);
    //    }

    //    public static bool NotEquals<T1, T2>(this ValueTuple<T1, T2> value, ValueTuple<T1, T2> other)
    //    {
    //        return !EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1) ||
    //               !EqualityComparer<T2>.Default.Equals(value.Item2, other.Item2);
    //    }

    //    public static bool Equals<T1, T2, T3>(this ValueTuple<T1, T2, T3> value, ValueTuple<T1, T2, T3> other)
    //    {
    //        return EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1) &&
    //               EqualityComparer<T2>.Default.Equals(value.Item2, other.Item2) &&
    //               EqualityComparer<T3>.Default.Equals(value.Item3, other.Item3);
    //    }

    //    public static bool NotEquals<T1, T2, T3>(this ValueTuple<T1, T2, T3> value, ValueTuple<T1, T2, T3> other)
    //    {
    //        return !EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1) ||
    //               !EqualityComparer<T2>.Default.Equals(value.Item2, other.Item2) ||
    //               !EqualityComparer<T3>.Default.Equals(value.Item3, other.Item3);
    //    }

    //    public static bool Equals<T1, T2, T3, T4>(this ValueTuple<T1, T2, T3, T4> value, ValueTuple<T1, T2, T3, T4> other)
    //    {
    //        return EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1) &&
    //               EqualityComparer<T2>.Default.Equals(value.Item2, other.Item2) &&
    //               EqualityComparer<T3>.Default.Equals(value.Item3, other.Item3) &&
    //               EqualityComparer<T4>.Default.Equals(value.Item4, other.Item4);
    //    }

    //    public static bool NotEquals<T1, T2, T3, T4>(this ValueTuple<T1, T2, T3, T4> value, ValueTuple<T1, T2, T3, T4> other)
    //    {
    //        return !EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1) ||
    //               !EqualityComparer<T2>.Default.Equals(value.Item2, other.Item2) ||
    //               !EqualityComparer<T3>.Default.Equals(value.Item3, other.Item3) ||
    //               !EqualityComparer<T4>.Default.Equals(value.Item4, other.Item4);
    //    }

    //    public static bool Equals<T1, T2, T3, T4, T5>(this ValueTuple<T1, T2, T3, T4, T5> value, ValueTuple<T1, T2, T3, T4, T5> other)
    //    {
    //        return EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1) &&
    //               EqualityComparer<T2>.Default.Equals(value.Item2, other.Item2) &&
    //               EqualityComparer<T3>.Default.Equals(value.Item3, other.Item3) &&
    //               EqualityComparer<T4>.Default.Equals(value.Item4, other.Item4) &&
    //               EqualityComparer<T5>.Default.Equals(value.Item5, other.Item5);
    //    }

    //    public static bool NotEquals<T1, T2, T3, T4, T5>(this ValueTuple<T1, T2, T3, T4, T5> value, ValueTuple<T1, T2, T3, T4, T5> other)
    //    {
    //        return !EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1) ||
    //               !EqualityComparer<T2>.Default.Equals(value.Item2, other.Item2) ||
    //               !EqualityComparer<T3>.Default.Equals(value.Item3, other.Item3) ||
    //               !EqualityComparer<T4>.Default.Equals(value.Item4, other.Item4) ||
    //               !EqualityComparer<T5>.Default.Equals(value.Item5, other.Item5);
    //    }
    //    public static bool Equals<T1, T2, T3, T4, T5, T6>(this ValueTuple<T1, T2, T3, T4, T5, T6> value, ValueTuple<T1, T2, T3, T4, T5, T6> other)
    //    {
    //        return EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1) &&
    //               EqualityComparer<T2>.Default.Equals(value.Item2, other.Item2) &&
    //               EqualityComparer<T3>.Default.Equals(value.Item3, other.Item3) &&
    //               EqualityComparer<T4>.Default.Equals(value.Item4, other.Item4) &&
    //               EqualityComparer<T5>.Default.Equals(value.Item5, other.Item5) &&
    //               EqualityComparer<T6>.Default.Equals(value.Item6, other.Item6);
    //    }

    //    public static bool NotEquals<T1, T2, T3, T4, T5, T6>(this ValueTuple<T1, T2, T3, T4, T5, T6> value, ValueTuple<T1, T2, T3, T4, T5, T6> other)
    //    {
    //        return !EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1) ||
    //               !EqualityComparer<T2>.Default.Equals(value.Item2, other.Item2) ||
    //               !EqualityComparer<T3>.Default.Equals(value.Item3, other.Item3) ||
    //               !EqualityComparer<T4>.Default.Equals(value.Item4, other.Item4) ||
    //               !EqualityComparer<T5>.Default.Equals(value.Item5, other.Item5) ||
    //               !EqualityComparer<T6>.Default.Equals(value.Item6, other.Item6);
    //    }

    //    public static bool Equals<T1, T2, T3, T4, T5, T6, T7>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7> value, ValueTuple<T1, T2, T3, T4, T5, T6, T7> other)
    //    {
    //        return EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1) &&
    //               EqualityComparer<T2>.Default.Equals(value.Item2, other.Item2) &&
    //               EqualityComparer<T3>.Default.Equals(value.Item3, other.Item3) &&
    //               EqualityComparer<T4>.Default.Equals(value.Item4, other.Item4) &&
    //               EqualityComparer<T5>.Default.Equals(value.Item5, other.Item5) &&
    //               EqualityComparer<T6>.Default.Equals(value.Item6, other.Item6) &&
    //               EqualityComparer<T7>.Default.Equals(value.Item7, other.Item7);
    //    }

    //    public static bool NotEquals<T1, T2, T3, T4, T5, T6, T7>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7> value, ValueTuple<T1, T2, T3, T4, T5, T6, T7> other)
    //    {
    //        return !EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1) ||
    //               !EqualityComparer<T2>.Default.Equals(value.Item2, other.Item2) ||
    //               !EqualityComparer<T3>.Default.Equals(value.Item3, other.Item3) ||
    //               !EqualityComparer<T4>.Default.Equals(value.Item4, other.Item4) ||
    //               !EqualityComparer<T5>.Default.Equals(value.Item5, other.Item5) ||
    //               !EqualityComparer<T6>.Default.Equals(value.Item6, other.Item6) ||
    //               !EqualityComparer<T7>.Default.Equals(value.Item7, other.Item7);
    //    }
    //    public static bool Equals<T1, T2, T3, T4, T5, T6, T7, T8>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8> value, ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8> other)
    //    {
    //        return EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1) &&
    //               EqualityComparer<T2>.Default.Equals(value.Item2, other.Item2) &&
    //               EqualityComparer<T3>.Default.Equals(value.Item3, other.Item3) &&
    //               EqualityComparer<T4>.Default.Equals(value.Item4, other.Item4) &&
    //               EqualityComparer<T5>.Default.Equals(value.Item5, other.Item5) &&
    //               EqualityComparer<T6>.Default.Equals(value.Item6, other.Item6) &&
    //               EqualityComparer<T7>.Default.Equals(value.Item7, other.Item7);
    //    }

    //    public static bool NotEquals<T1, T2, T3, T4, T5, T6, T7, T8>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8> value, ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8> other)
    //    {
    //        return !EqualityComparer<T1>.Default.Equals(value.Item1, other.Item1) ||
    //               !EqualityComparer<T2>.Default.Equals(value.Item2, other.Item2) ||
    //               !EqualityComparer<T3>.Default.Equals(value.Item3, other.Item3) ||
    //               !EqualityComparer<T4>.Default.Equals(value.Item4, other.Item4) ||
    //               !EqualityComparer<T5>.Default.Equals(value.Item5, other.Item5) ||
    //               !EqualityComparer<T6>.Default.Equals(value.Item6, other.Item6) ||
    //               !EqualityComparer<T7>.Default.Equals(value.Item7, other.Item7);
    //    }
    //}
}