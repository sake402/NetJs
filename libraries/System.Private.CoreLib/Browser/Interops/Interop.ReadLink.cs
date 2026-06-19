using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

internal static partial class Interop
{
    internal static unsafe partial class Sys
    {
        private static partial int ReadLink(ref byte path, ref byte buffer, int bufferSize)
        {
            return -1;
        }
    }
}
