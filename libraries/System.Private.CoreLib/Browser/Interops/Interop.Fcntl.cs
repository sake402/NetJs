// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static partial class Fcntl
        {
#if MEMORY_MAPPED_FILES
            internal static partial int DangerousSetIsNonBlocking(IntPtr fd, int isNonBlocking)
            {
                return -1;
            }

            internal static partial int SetIsNonBlocking(SafeHandle fd, int isNonBlocking)
            {
                return -1;
            }

            internal static partial int GetIsNonBlocking(SafeHandle fd,  out bool isNonBlocking)
            {
                isNonBlocking = false;
                return -1;
            }

            internal static partial int SetFD(SafeHandle fd, int flags)
            {
                return -1;
            }

            internal static partial int GetFD(SafeHandle fd)
            {
                return -1;
            }

            internal static partial int GetFD(IntPtr fd)
            {
                return -1;
            }
#endif
        }
    }
}
