using NetJs;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Text;

internal static partial class Interop
{
    internal static unsafe partial class Sys
    {
        private static unsafe partial byte* GetCwd(byte* buffer, int bufferLength)
        {
            RefOrPointer<byte> ptr = Script.Ref(buffer);
            "/".TryCopyTo(new Span<char>((void*)buffer, bufferLength));
            return buffer;
        }
    }
}
