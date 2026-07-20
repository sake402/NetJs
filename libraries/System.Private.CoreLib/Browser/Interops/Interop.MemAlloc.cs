using NetJs;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

internal static partial class Interop
{
    internal static unsafe partial class Sys
    {
         internal static partial void* AlignedAlloc(nuint alignment, nuint size)
        {
            var arr = new object[size];
            var refs = RuntimeHelpers.CreateArrayReferenceT(arr, -1);
            return Script.RefP(refs);
        }

        internal static partial void AlignedFree(void* ptr)
        {

        }

        internal static partial void* AlignedRealloc(void* ptr, nuint alignment, nuint new_size)
        {
            var refs = Script.Ref((object*)ptr);
            var arr = refs._array;
            Array.Resize<object>(ref arr, (int)new_size);
            refs._dataSource = arr;
            return Script.RefP(refs);
        }
        
        internal static partial void* Calloc(nuint num, nuint size)
        {
            var arr = new object[size.As<int>()];
            var refs = RuntimeHelpers.CreateArrayReferenceT(arr, -1);
            return Script.RefP(refs);
        }

        internal static partial void Free(void* ptr)
        {

        }

        internal static partial void* Malloc(nuint size)
        {
            var arr = new object[size.As<int>()];
            var refs = RuntimeHelpers.CreateArrayReferenceT(arr, -1);
            return Script.RefP(refs);
        }

        internal static partial void* Realloc(void* ptr, nuint new_size)
        {
            var refs = Script.Ref((object*)ptr);
            var arr = refs._array;
            Array.Resize<object>(ref arr, (int)new_size);
            refs._dataSource = arr;
            return Script.RefP(refs);
        }
    }
}
