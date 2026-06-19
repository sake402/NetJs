using NetJs;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

internal static partial class Interop
{
    internal static unsafe partial class Sys
    {



        internal static unsafe partial long PWriteV(SafeHandle fd, IOVector* vectors, int vectorCount, long fileOffset)
        {
            return -1;
        }

        internal static partial IntPtr OpenDir(string path)
        {
            return -1;
        }

        internal static unsafe partial int ReadDir(IntPtr dir, DirectoryEntry* outputEntry)
        {
            return -1;
        }

        internal static partial int CloseDir(IntPtr dir)
        {
            return -1;
        }

        internal static partial int RmDir(string path)
        {
            return -1;
        }
        internal static partial int SymLink(string target, string linkPath)
        {
            return -1;
        }

        internal static partial int Access(string path, AccessMode mode)
        {
            return -1;
        }
        internal static partial int CopyFile(SafeFileHandle source, SafeFileHandle destination, long sourceLength)
        {
            return -1;
        }
        internal static partial int FAllocate(SafeFileHandle fd, long offset, long length)
        {
            return -1;
        }

        internal static partial int SchedGetCpu()
        {
            return 0;
        }

    }
}
