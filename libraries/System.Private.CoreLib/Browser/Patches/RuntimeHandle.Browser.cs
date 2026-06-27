using System;
using System.Collections.Generic;
using System.Text;

namespace Mono
{
    [NetJs.ForcePartial(typeof(RuntimeClassHandle))]
    //[NetJs.Boot]
    //[NetJs.Reflectable(false)]
    //[NetJs.OutputOrder(int.MinValue + 1)] //make sure we emit this type immediately after AppDomain
    internal unsafe partial struct RuntimeClassHandle_Partial
    {
        [NetJs.MemberReplace]
        internal static unsafe IntPtr GetTypeFromClass(RuntimeStructs.MonoClass* klass)
        {
            return System.Runtime.InteropServices.Marshal.MarshalObject(klass);
        }

    }
}
