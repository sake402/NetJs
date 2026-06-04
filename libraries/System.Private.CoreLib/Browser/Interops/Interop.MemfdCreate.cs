using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Text;

#if SYSTEM_PRIVATE_CORELIB
#else
internal static partial class Interop
{
    internal static unsafe partial class Sys
    {
        internal static partial SafeFileHandle MemfdCreate(string name, int isReadonly)
        {
            throw new PlatformNotSupportedException();
        }

        private static partial int MemfdSupportedImpl()
        {
            return 0;
        }

    }
}
#endif