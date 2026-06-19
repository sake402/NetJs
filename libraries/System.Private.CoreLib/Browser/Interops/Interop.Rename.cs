using System;
using System.Collections.Generic;
using System.Text;
internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static partial int Rename(string oldPath, string newPath)
        {
            return -1;
        }

        internal static partial int Rename(ref byte oldPath, ref byte newPath)
        {
            return -1;
        }
    }
}
