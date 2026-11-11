using NetJs;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Window;

namespace System
{
    [NetJs.StaticCallConvention]
    public partial class Enum
    {
        [NetJs.MemberReplace(nameof(GetEnumValuesAndNames))]
        private static void GetEnumValuesAndNamesImpl(QCallTypeHandle enumType, out ulong[] values, out string[] names)
        {
            var prototype = enumType.QCallTypeHandleToRuntimeType()._prototype.As<EnumPrototype>();
            names = (prototype["$enumNames"] ??= prototype.Map.Keys.AsNetArray()).As<string[]>();
            values = (prototype["$enumValues"] ??= prototype.Map.Values.AsNetArray()).As<ulong[]>();
        }

        [NetJs.MemberReplace(nameof(InternalGetCorElementType))]
        private static CorElementType InternalGetCorElementTypeImpl(QCallTypeHandle enumType)
        {
            var prototype = enumType.QCallTypeHandleToRuntimeType()._prototype.As<EnumPrototype>();
            var type = prototype.UnderlyingType.Type.As<RuntimeType>();
            return RuntimeTypeHandle.GetCorElementType(new QCallTypeHandle(ref type));
        }

        [NetJs.MemberReplace(nameof(InternalGetUnderlyingType))]
        private static void InternalGetUnderlyingTypeImpl(QCallTypeHandle enumType, ObjectHandleOnStack res)
        {
            var prototype = enumType.QCallTypeHandleToRuntimeType()._prototype.As<EnumPrototype>();
            res.GetObjectHandleOnStack<Type?>() = prototype.UnderlyingType.Type;
        }

        [NetJs.MemberReplace(nameof(HasFlag))]
        [NetJs.Template("(({this} & ({flag})) == ({flag}))")]
        public extern bool HasFlagImpl(Enum flag);
        //{
        //    var thisV = this.As<int>();
        //    var flagV = flag.As<int>();
        //    return (thisV & flagV) != 0;
        //}

        [NetJs.Name(NetJs.Constants.IsTypeName)]
        public static bool Is(object value, NativeAction<object>? result)
        {
            var unboxed = NetJs.Script.Unbox(value);
            var t = NetJs.Script.TypeOf(unboxed);
            EnumPrototype prototype = NetJs.Script.Write<EnumPrototype>("this");
            var underlyingType = prototype.UnderlyingType;
            if (NetJs.Script.IsUndefined(underlyingType)) // System.Enum itself
            {
                if (value == unboxed) //value not boxed
                {
                    return false;
                }
            }
            if (NetJs.Script.IsDefined(underlyingType) && (t.NativeEquals("number") || t.NativeEquals("bigint")))
            {
                if (result != null)
                {
                    if (t.NativeEquals("number") && (underlyingType.KnownType == KnownTypeHandle.SystemInt64 || underlyingType.KnownType == KnownTypeHandle.SystemUInt64))
                    {
                        result(NetJs.Script.Write<object>("BigInt(unboxed)"));
                    }
                    if (t.NativeEquals("bigint") && underlyingType.KnownType != KnownTypeHandle.SystemInt64 && underlyingType.KnownType != KnownTypeHandle.SystemUInt64)
                    {
                        result(NetJs.Script.Write<object>("Number(unboxed)"));
                    }
                }
                return true;
            }
            return false;
        }

        public static string ToStringT<TEnum, TStorage>(TStorage value)
            where TEnum : struct
            where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>
        {
            EnumInfo<TStorage> enumInfo = GetEnumInfo<TStorage>(typeof(TEnum).As<RuntimeType>());
            string? result = enumInfo.HasFlagsAttribute ?
                FormatFlagNames(enumInfo, value) :
                GetNameInlined(enumInfo, value);
            return result ?? (typeof(TStorage) == typeof(ulong) ? value.As<ulong>().ToString() : typeof(TStorage) == typeof(long) ? value.As<long>().ToString() : value.As<uint>().ToString()!);
        }

        public static new int GetHashCodeT<TStorage>(TStorage enumValue)
        {
            if (typeof(TStorage) == typeof(long) || typeof(TStorage) == typeof(ulong))
            {
                var lvalue = enumValue.As<long>();
                return (int)lvalue ^ (int)(lvalue >> 32);
            }
            return enumValue.As<int>() | 0;
        }

        public static int CompareToT<TStorage>(TStorage enumValue1, TStorage enumValue2)
        {
            if (typeof(TStorage) == typeof(long) || typeof(TStorage) == typeof(ulong))
            {
                var lvalue1 = enumValue1.As<long>();
                var lvalue2 = enumValue2.As<long>();
                return (int)(lvalue1 - lvalue2);
            }
            return enumValue1.As<int>() - enumValue2.As<int>();
        }

        public static Enum? operator &([NetJs.Box(false)] Enum? left, [NetJs.Box(false)] Enum? right)
        {
            if (left == null || right == null)
                return null;
            if (NetJs.Script.TypeOf(left).NativeEquals("number"))
                return (left.As<int>() & right.As<int>()).As<Enum>();
            return (left.As<long>() & right.As<long>()).As<Enum>();
        }

        public static Enum? operator |([NetJs.Box(false)] Enum? left, [NetJs.Box(false)] Enum? right)
        {
            if (left == null || right == null)
                return null;
            if (NetJs.Script.TypeOf(left).NativeEquals("number"))
                return (left.As<int>() | right.As<int>()).As<Enum>();
            return (left.As<long>() | right.As<long>()).As<Enum>();
        }
    }

    /// <summary>
    /// This type prevent boxing on enum before ToString, GetHashCode
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <typeparam name="TStorage"></typeparam>
    [NetJs.Base(typeof(Enum))]
    [NetJs.StaticCallConvention]
    public class Enum<TEnum, TStorage>
        where TEnum : struct
        where TStorage : struct, INumber<TStorage>, IBitwiseOperators<TStorage, TStorage, TStorage>
    {
        public override string ToString()
        {
            var unboxed = NetJs.Script.Unbox(this);
            return Enum.ToStringT<TEnum, TStorage>(unboxed.As<TStorage>());
        }

        public override int GetHashCode()
        {
            var unboxed = NetJs.Script.Unbox(this);
            return Enum.GetHashCodeT<TStorage>(unboxed.As<TStorage>());
        }
    }
}
