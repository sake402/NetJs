using System;
using System.Collections.Generic;
using System.Text;

#if SYSTEM_PRIVATE_CORELIB
#else
internal static partial class Interop
{
    internal static unsafe partial class Sys
    {
        internal static partial int MAdvise(IntPtr addr, ulong length, MemoryAdvice advice)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
#endif