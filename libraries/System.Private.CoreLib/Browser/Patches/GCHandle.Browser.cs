using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.InteropServices
{
    public partial struct GCHandle
    {
        [NetJs.MemberReplace(nameof(InternalAlloc))]
        internal static IntPtr InternalAllocImpl(object? value, GCHandleType type)
        {
            //Handle should be aligned by 4
            var handle = Marshal.MarshalObject(value);
            //if ((handle & 3) == 0)
            //    return handle;
            //var n = handle & unchecked((int)~InteropUtility.virtualAddressOffset);
            //n <<= 2;
            //handle = (n.As<uint>() | InteropUtility.virtualAddressOffset).As<IntPtr>();
            return handle;
        }

        [NetJs.MemberReplace(nameof(InternalSet))]
        internal static void InternalSetImpl(IntPtr handle, object? value)
        {
            //we ensured it was aligned above
            //if ((handle & 3) != 0)
            //{
            //    var n = handle & unchecked((int)~InteropUtility.virtualAddressOffset);
            //    n >>= 2;
            //    handle = (n.As<uint>() | InteropUtility.virtualAddressOffset).As<IntPtr>();
            //}
            Marshal.MarshalObject(value, handle, overwriteExisting: true);
        }

        [NetJs.MemberReplace(nameof(InternalFree))]
        internal static void InternalFreeImpl(IntPtr handle)
        {
            //we ensured it was aligned above
            //var n = handle & unchecked((int)~InteropUtility.virtualAddressOffset);
            //n >>= 2;
            //handle = (n.As<uint>() | InteropUtility.virtualAddressOffset).As<IntPtr>();
            Marshal.Remove(handle);
        }

        [NetJs.MemberReplace(nameof(InternalGet))]
        internal static object? InternalGetImpl(IntPtr handle)
        {
            //we ensured it was aligned above
            //var n = handle & unchecked((int)~InteropUtility.virtualAddressOffset);
            //n >>= 2;
            //handle = (n.As<uint>() | InteropUtility.virtualAddressOffset).As<IntPtr>();
            return Marshal.MarshalObject(handle);
        }

    }
}
