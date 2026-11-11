using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    [NetJs.ForcePartial(typeof(ByReference))]
    internal readonly ref struct ByReference_Partial
    {
        [NetJs.MemberReplace("Create<>(ref T)")]
        [NetJs.Template("{0}")]
        public static ByReference Create<T>(ref T p) => NetJs.Script.Write<ByReference>("p");
    }
}
