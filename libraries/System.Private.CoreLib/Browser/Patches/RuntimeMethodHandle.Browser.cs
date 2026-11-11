using NetJs;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    [NetJs.ForcePartial(typeof(RuntimeMethodHandle))]
    //[NetJs.Boot]
    //[NetJs.Reflectable(false)]
    //[NetJs.OutputOrder(int.MinValue + 1)] //make sure we emit this type immediately after AppDomain
    public partial struct RuntimeMethodHandle_Partial
    {
        [NetJs.MemberReplace]
        private static IntPtr GetFunctionPointer(IntPtr m)
        {
            return m;
        }

        [NetJs.MemberReplace]
        private static void ReboxFromNullable(object? src, ObjectHandleOnStack res)
        {
            unchecked
            {
                if (src == null)
                {
                    res.GetObjectHandleOnStack<object?>() = null;
                    return;
                }
                var nullableType = src.GetType().As<RuntimeType>();
                var objectType = nullableType._prototype.Arguments![0];
                var nullableValue = src.As<Nullable<int>>()!.Value.As<object>();
                res.GetObjectHandleOnStack<object?>() = NetJs.Script.Box(nullableValue, objectType).As<object>();
            }
        }

        [NetJs.MemberReplace]
        private static void ReboxToNullable(object? src, QCallTypeHandle destNullableType, ObjectHandleOnStack res)
        {
            unchecked
            {
                var nullableType = destNullableType.QCallTypeHandleToRuntimeType();
                var value = NetJs.Script.Unbox(src);
                var ctor = nullableType._prototype.Metadata!.Constructors!.filter(e => NetJs.Script.IsDefined(e.Parameters) && e.Parameters!.Length == 1)[0];
                var nullable = nullableType._prototype.New();
                var ctorMethod = nullable[ctor.GetOutputName()].As<NativeAction<object>>();
                ctorMethod.InvokeCall(nullable, value);
                res.GetObjectHandleOnStack<object?>() = nullable;
            }
        }

    }
}
