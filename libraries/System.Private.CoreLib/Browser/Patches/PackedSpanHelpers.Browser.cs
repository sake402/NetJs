using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace System
{
    internal static partial class PackedSpanHelpers
    {
        [NetJs.MemberReplace(nameof(PackedIndexOfIsSupported))]
        public static bool PackedIndexOfIsSupportedImpl => false;
    }
}
