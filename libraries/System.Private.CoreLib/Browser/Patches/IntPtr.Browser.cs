
namespace System
{
    [NetJs.ForcePartial(typeof(IntPtr))]
    [NetJs.StaticCallConvention]
    public readonly partial struct IntPtr_Partial
    {
        //[NetJs.MemberReplace(nameof(GetHashCode))]
        //[NetJs.Template("{global.}" + NetJs.Constants.GetHashCodeName + "({this:!super})")] //make sure we dont pass super keyword in here. JS doesnt support it
        //public int GetHashCodeImplChar()
        //{
        //    return this.As<int>();
        //}

        readonly nint _m_value;
        [NetJs.MemberReplace("_value")]
        internal nint MValue
        {
            get
            {
                if (NetJs.Script.TypeOf(this).NativeEquals("number"))
                    return this.As<int>();
                return _m_value;
            }
            set
            {
                NetJs.Script.Write("this._m_value = value");
            }
        }
    }
}
