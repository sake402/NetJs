
namespace System
{
    [NetJs.ForcePartial(typeof(UInt32))]
    [NetJs.StaticCallConvention]
    public readonly partial struct UInt32_Partial
    {
        //[NetJs.MemberReplace(nameof(GetHashCode))]
        //public int GetHashCodeImplUInt32()
        //{
        //    return (int)NetJs.Script.Write<uint>("m_value");
        //}

        readonly uint _m_value;
        [NetJs.MemberReplace("m_value")]
        internal uint MValue
        {
            get
            {
                if (NetJs.Script.TypeOf(this).NativeEquals("number"))
                    return this.As<uint>();
                return _m_value;
            }
            set
            {
                NetJs.Script.Write("this._m_value = value");
            }
        }
    }
}
