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
            res.GetObjectHandleOnStack<object?>() = src;
        }

        [NetJs.MemberReplace]
        private static void ReboxToNullable(object? src, QCallTypeHandle destNullableType, ObjectHandleOnStack res)
        {
            res.GetObjectHandleOnStack<object?>() = src;
        }

    }
}
