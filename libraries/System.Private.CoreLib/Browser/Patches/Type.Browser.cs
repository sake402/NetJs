using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    public abstract partial class Type
    {
        [NetJs.MemberReplace(nameof(internal_from_handle))]
        private static Type internal_from_handleImpl(IntPtr handle)
        {
            return RuntimeType.GetTypeFromHandle((uint)handle) ?? throw new InvalidOperationException("Invalid handle");
        }
    }
}
