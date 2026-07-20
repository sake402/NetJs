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
            var n = handle & (int)~InteropUtility.virtualObjectAddressOffset;
            n <<=2;
            handle = (n.As<uint>() | InteropUtility.virtualObjectAddressOffset).As<IntPtr>();
            return handle;
        }

        [NetJs.MemberReplace(nameof(InternalSet))]
        internal static void InternalSetImpl(IntPtr handle, object? value)
        {
            //we ensured it was aligned above
            var n = handle & (int)~InteropUtility.virtualObjectAddressOffset;
            n >>= 2;
            handle = (n.As<uint>() | InteropUtility.virtualObjectAddressOffset).As<IntPtr>();
            Marshal.MarshalObject(value, handle);
        }

        [NetJs.MemberReplace(nameof(InternalFree))]
        internal static void InternalFreeImpl(IntPtr handle)
        {
            //we ensured it was aligned above
            var n = handle & (int)~InteropUtility.virtualObjectAddressOffset;
            n >>= 2;
            handle = (n.As<uint>() | InteropUtility.virtualObjectAddressOffset).As<IntPtr>();
            Marshal.Remove(handle);
        }

        [NetJs.MemberReplace(nameof(InternalGet))]
        internal static object? InternalGetImpl(IntPtr handle)
        {
            //we ensured it was aligned above
            var n = handle & (int)~InteropUtility.virtualObjectAddressOffset;
            n >>= 2;
            handle = (n.As<uint>() | InteropUtility.virtualObjectAddressOffset).As<IntPtr>();
            return Marshal.MarshalObject(handle);
        }

    }
}
