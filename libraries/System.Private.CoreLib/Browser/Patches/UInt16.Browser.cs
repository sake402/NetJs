namespace System
{
    [NetJs.ForcePartial(typeof(UInt16))]
    [NetJs.StaticCallConvention]
    public readonly struct UInt16_Partial 
    {
        //[NetJs.MemberReplace(nameof(GetHashCode))]
        //public int GetHashCodeImplUInt16()
        //{
        //    return NetJs.Script.Write<ushort>("m_value");
        //}

        readonly ushort _m_value;
        [NetJs.MemberReplace("m_value")]
        internal ushort MValue
        {
            get
            {
                if (NetJs.Script.TypeOf(this).NativeEquals("number"))
                    return this.As<ushort>();
                return _m_value;
            }
            set
            {
                NetJs.Script.Write("this._m_value = value");
            }
        }
    }
}
