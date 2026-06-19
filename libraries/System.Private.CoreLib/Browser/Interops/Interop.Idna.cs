using System;
using System.Collections.Generic;
using System.Text;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        internal static  unsafe partial int ToAscii(uint flags,char* src, int srcLen, char* dstBuffer, int dstBufferCapacity)
        {
            int i = 0;
            for (i = 0; i < srcLen && i < dstBufferCapacity; i++)
            {
                if (src[i] <= 127)
                    dstBuffer[i] = src[i];
                else
                    dstBuffer[i] = '\0'; //TODO: Transliterating Unicode to ASCII to limit loss of information
            }
            return i;
        }

        internal static unsafe partial int ToUnicode(uint flags, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity)
        {
            int i = 0;
            for (i = 0; i < srcLen && i < dstBufferCapacity; i++)
            {
                dstBuffer[i] = src[i];
            }
            return i;
        }

    }
}
