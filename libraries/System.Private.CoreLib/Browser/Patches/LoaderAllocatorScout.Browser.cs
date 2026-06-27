using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection
{
    [NetJs.ForcePartial(typeof(LoaderAllocatorScout))]
    internal sealed partial class LoaderAllocatorScout_Partial
    {
        [NetJs.MemberReplace]
        private static bool Destroy(IntPtr native)
        {
            Marshal.Remove(native);
            return true;
        }
    }
}
