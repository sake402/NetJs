
namespace System
{
    [NetJs.ForcePartial(typeof(Int64))]
    [NetJs.StaticCallConvention]
    public readonly partial struct Int64_Partial
    {
        //[NetJs.MemberReplace(nameof(GetHashCode))]
        //public int GetHashCodeImplInt64()
        //{
        //    return (int)NetJs.Script.Write<long>("m_value");
        //}

        readonly long _m_value;
        [NetJs.MemberReplace("m_value")]
        internal long MValue
        {
            get
            {
                if (NetJs.Script.TypeOf(this).NativeEquals("bigint"))
                    return this.As<long>();
                return _m_value;
            }
            set
            {
                NetJs.Script.Write("this._m_value = value");
            }
        }

        //[NetJs.Name(Constants.IsTypeName)]
        //public static bool Is(object value)
        //{
        //    return NetJs.Script.TypeOf(value).NativeEquals("bigint");
        //}
    }
}
