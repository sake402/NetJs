using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

internal static partial class Interop
{
    internal static unsafe partial class Sys
    {
        private static partial int MkDir(ref byte path, int mode)
        {
            return 0;
        }
    }
}
