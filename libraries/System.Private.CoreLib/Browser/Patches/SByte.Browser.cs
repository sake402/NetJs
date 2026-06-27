namespace System
{
    [NetJs.ForcePartial(typeof(SByte))]
    [NetJs.StaticCallConvention]
    public readonly struct SByte_Partial 
    {
        [NetJs.MemberReplace(nameof(GetHashCode))]
        public int GetHashCodeImplChar()
        {
            return this.As<sbyte>();
        }

        readonly sbyte _m_value;
        [NetJs.MemberReplace("m_value")]
        internal sbyte MValue
        {
            get
            {
                if (NetJs.Script.TypeOf(this).NativeEquals("number"))
                    return this.As<sbyte>();
                return _m_value;
            }
            set
            {
                NetJs.Script.Write("this._m_value = value");
            }
        }
    }
}
