// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static unsafe partial int Read(SafeHandle fd, byte* buffer, int count)
        {
            return -1;
        }

        //internal static unsafe partial int ReadFromNonblocking(SafeHandle fd, byte* buffer, int count)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
