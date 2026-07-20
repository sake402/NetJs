using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    [NetJs.ForcePartial(typeof(Utf16Char))]
    [NetJs.StaticCallConvention]
    internal readonly partial struct Utf16Char_Partial
    {
        //readonly char _m_value;
        [NetJs.MemberReplace("value")]
        internal char MValue
        {
            get
            {
                if (NetJs.Script.TypeOf(this).NativeEquals("number"))
                    return this.As<char>();
                return field;
            }
            set
            {
                NetJs.Script.Write("this.value$ = value");
            }
        }
    }
}
