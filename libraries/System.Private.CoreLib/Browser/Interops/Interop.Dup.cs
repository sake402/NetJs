using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Text;

internal static partial class Interop
{
    internal static unsafe partial class Sys
    {
#if CONSOLE
        internal static partial SafeFileHandle Dup(SafeFileHandle oldfd)
        {
            return oldfd;
        }
#endif
    }
}
