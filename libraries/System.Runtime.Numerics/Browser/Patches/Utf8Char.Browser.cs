using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    [NetJs.ForcePartial(typeof(Utf8Char))]
    [NetJs.StaticCallConvention]
    internal readonly partial struct Utf8Char_Partial
    {
        //readonly byte _m_value;
        [NetJs.MemberReplace("value")]
        internal byte MValue
        {
            get
            {
                if (NetJs.Script.TypeOf(this).NativeEquals("number"))
                    return this.As<byte>();
                return field;
            }
            set
            {
                NetJs.Script.Write("this.value$ = value");
            }
        }
    }
}
