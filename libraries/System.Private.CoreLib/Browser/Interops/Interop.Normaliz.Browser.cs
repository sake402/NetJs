using NetJs;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Intrinsics;
using System.Text;

internal static partial class Interop
{
    internal static partial class Normaliz
    {

        internal static unsafe partial int IdnToAscii(
                                        uint dwFlags,
                                        char* lpUnicodeCharStr,
                                        int cchUnicodeChar,
                                        char* lpASCIICharStr,
                                        int cchASCIIChar)
        {
            return -1;
        }

        internal static unsafe partial int IdnToUnicode(
                                        uint dwFlags,
                                        char* lpASCIICharStr,
                                        int cchASCIIChar,
                                        char* lpUnicodeCharStr,
                                        int cchUnicodeChar)
        {
            return -1;
        }

        internal static unsafe partial BOOL IsNormalizedString(NormalizationForm normForm, char* source, int length)
        {
            return BOOL.FALSE;
        }

        internal static unsafe partial int NormalizeString(
                                        NormalizationForm normForm,
                                        char* source,
                                        int sourceLength,
                                        char* destination,
                                        int destinationLength)
        {
            return -1;
        }

    }
}
