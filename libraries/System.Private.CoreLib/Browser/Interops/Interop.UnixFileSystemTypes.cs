using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Text;

internal static partial class Interop
{
    internal static unsafe partial class Sys
    {
        private static partial uint GetFileSystemType(SafeFileHandle fd)
        {
            return 0;
        }

    }
}
