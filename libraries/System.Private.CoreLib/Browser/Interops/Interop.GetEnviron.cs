using System;
using System.Collections.Generic;
using System.Text;

internal static partial class Interop
{
    internal static unsafe partial class Sys
    {
        internal static unsafe partial IntPtr GetEnviron()
        {
            return IntPtr.Zero;
        }

        internal static unsafe partial void FreeEnviron(IntPtr environ)
        {
        }

    }
}
