using System;

namespace System
{
    [NetJs.ForcePartial(typeof(Byte))]
    [NetJs.StaticCallConvention]
    public readonly struct Byte_Partial 
    {
        //[NetJs.MemberReplace(nameof(GetHashCode))]
        //public int GetHashCodeImplChar()
        //{
        //    return NetJs.Script.Write<int>("m_value");
        //}

        readonly byte _m_value;
        [NetJs.MemberReplace("m_value")]
        internal byte MValue
        {
            get
            {
                if (NetJs.Script.TypeOf(this).NativeEquals("number"))
                    return this.As<byte>();
                return _m_value;
            }
            set
            {
                NetJs.Script.Write("this._m_value = value");
            }
        }
    }
}
