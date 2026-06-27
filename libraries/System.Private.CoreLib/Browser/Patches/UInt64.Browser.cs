
namespace System
{
    [NetJs.ForcePartial(typeof(UInt64))]
    [NetJs.StaticCallConvention]
    public readonly partial struct UInt64_Partial
    {
        //[NetJs.MemberReplace(nameof(GetHashCode))]
        //public int GetHashCodeImplUInt64()
        //{
        //    return (int)NetJs.Script.Write<ulong>("m_value");
        //}

        readonly ulong _m_value;
        [NetJs.MemberReplace("m_value")]
        internal ulong MValue
        {
            get
            {
                if (NetJs.Script.TypeOf(this).NativeEquals("bigint"))
                    return this.As<ulong>();
                return _m_value;
            }
            set
            {
                NetJs.Script.Write("this._m_value = value");
            }
        }
    }
}
