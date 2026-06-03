using System.Collections.Generic;

namespace System
{
    [NetJs.ForcePartial(typeof(ValueType))]
    [NetJs.Boot]
    [NetJs.OutputOrder(int.MinValue + 2)]
    public abstract partial class ValueType_Partial
    {
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