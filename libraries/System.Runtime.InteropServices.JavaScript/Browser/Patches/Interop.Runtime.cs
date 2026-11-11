using System;
using System.Collections.Generic;
using System.Text;

internal static partial class Interop
{
    internal static unsafe partial class Runtime
    {
        [NetJs.MemberReplace(nameof(ReleaseCSOwnedObject))]
        internal static void ReleaseCSOwnedObjectImpl(nint jsHandle)
        {

        }
    }
}
