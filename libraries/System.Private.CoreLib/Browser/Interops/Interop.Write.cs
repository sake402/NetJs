// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static unsafe partial int Write(SafeHandle fd, byte* buffer, int bufferSize)
        {
            var reff = NetJs.Script.Ref(buffer);
            var array = reff.ToArray(bufferSize);
            NetJs.Script.Write("const uint8Array = new Uint8Array(array)");
            NetJs.Script.Write("const decodedString = new TextDecoder().decode(uint8Array)");
            NetJs.Script.Write("console.log(decodedString)");
            return bufferSize;
        }

        internal static unsafe partial int Write(IntPtr fd, byte* buffer, int bufferSize)
        {
            return -1;
        }

        //internal static unsafe partial int WriteToNonblocking(SafeHandle fd, byte* buffer, int bufferSize)
        //{
        //    return -1;
        //}
    }
}
