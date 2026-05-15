using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    [NetJs.StaticCallConvention]
    public partial class Enum
    {
        [NetJs.MemberReplace(nameof(GetEnumValuesAndNames))]
        private static void GetEnumValuesAndNamesImpl(QCallTypeHandle enumType, out ulong[] values, out string[] names)
        {
            var prototype = enumType.QCallTypeHandleToRuntimeType()._prototype.As<EnumPrototype>();
            names = prototype.Map.Keys;
            values = prototype.Map.Values.As<ulong[]>();
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
        [NetJs.Template("({this} & {flag}) != 0")]
        public extern bool HasFlagImpl(Enum flag);
        //{
        //    var thisV = this.As<int>();
        //    var flagV = flag.As<int>();
        //    return (thisV & flagV) != 0;
        //}

        [NetJs.Name(NetJs.Constants.IsTypeName)]
        public static bool Is(object value)
        {
            var t = NetJs.Script.TypeOf(value);
            return t.NativeEquals("number") || t.NativeEquals("bigint");
        }
    }
}
