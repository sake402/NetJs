namespace System
{
    [NetJs.ForcePartial(typeof(Double))]
    [NetJs.StaticCallConvention]
    public readonly struct Double_Partial
    {
        //[NetJs.MemberReplace(nameof(GetHashCode))]
        //public int GetHashCodeImplChar()
        //{
        //    return NetJs.Script.Write<short>("m_value");
        //}

        readonly double _m_value;
        [NetJs.MemberReplace("m_value")]
        internal double MValue
        {
            get
            {
                if (NetJs.Script.TypeOf(this).NativeEquals("number"))
                    return this.As<double>();
                return _m_value;
            }
            set
            {
                NetJs.Script.Write("this._m_value = value");
            }
        }
    }
}
