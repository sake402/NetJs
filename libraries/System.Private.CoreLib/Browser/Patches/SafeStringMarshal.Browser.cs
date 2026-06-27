using System;
using System.Collections.Generic;
using System.Text;

namespace Mono
{
    [NetJs.ForcePartial(typeof(SafeStringMarshal))]
    internal partial struct SafeStringMarshal_Partial
    {
        [NetJs.MemberReplace]
        private static IntPtr StringToUtf8_icall(ref string str)
        {
            return System.Runtime.InteropServices.Marshal.MarshalObject(str);
        }

        [NetJs.MemberReplace]
        public static void GFree(IntPtr ptr)
        {
            System.Runtime.InteropServices.Marshal.Remove(ptr);
        }
    }
}
