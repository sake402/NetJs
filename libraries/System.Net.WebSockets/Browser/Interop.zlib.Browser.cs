// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class ZLib
    {
        internal static unsafe partial ZLibNative.ErrorCode DeflateInit2_(
            ZLibNative.ZStream* stream,
            ZLibNative.CompressionLevel level,
            ZLibNative.CompressionMethod method,
            int windowBits,
            int memLevel,
            ZLibNative.CompressionStrategy strategy)
        {
            return ZLibNative.ErrorCode.StreamError;
        }

        internal static unsafe partial ZLibNative.ErrorCode Deflate(ZLibNative.ZStream* stream, ZLibNative.FlushCode flush)
        {
            return ZLibNative.ErrorCode.StreamError;
        }

        internal static unsafe partial ZLibNative.ErrorCode DeflateEnd(ZLibNative.ZStream* stream)
        {
            return ZLibNative.ErrorCode.StreamError;
        }

        internal static unsafe partial ZLibNative.ErrorCode InflateInit2_(ZLibNative.ZStream* stream, int windowBits)
        {
            return ZLibNative.ErrorCode.StreamError;
        }

        internal static unsafe partial ZLibNative.ErrorCode InflateReset2_(ZLibNative.ZStream* stream, int windowBits)
        {
            return ZLibNative.ErrorCode.StreamError;
        }

        internal static unsafe partial ZLibNative.ErrorCode Inflate(ZLibNative.ZStream* stream, ZLibNative.FlushCode flush)
        {
            return ZLibNative.ErrorCode.StreamError;
        }

        internal static unsafe partial ZLibNative.ErrorCode InflateEnd(ZLibNative.ZStream* stream)
        {
            return ZLibNative.ErrorCode.StreamError;
        }

        internal static unsafe partial uint crc32(uint crc, byte* buffer, int len)
        {
            return 0;
        }
    }
}
