
namespace System
{
    [NetJs.ForcePartial(typeof(Int32))]
    [NetJs.StaticCallConvention]
    public readonly partial struct Int32_Partial
    {
        //[NetJs.MemberReplace(nameof(GetHashCode))]
        //public int GetHashCodeImplInt32()
        //{
        //    return NetJs.Script.Write<int>("m_value");
        //}

        readonly int _m_value;
        [NetJs.MemberReplace("m_value")]
        internal int MValue
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

        public static Int32_Partial? operator +(Int32_Partial a, Int32_Partial? b)
        {
            if (b.As<int?>() == null)
                return null;
            return (a.As<int>() + b.As<int>()).As<Int32_Partial>();
        }

        public static Int32_Partial? operator +(Int32_Partial? a, Int32_Partial? b)
        {
            if (a.As<int?>() == null || b.As<int?>() == null)
                return null;
            return (a.As<int>() + b.As<int>()).As<Int32_Partial>();
        }

        public static Int32_Partial? operator -(Int32_Partial a, Int32_Partial? b)
        {
            if (b.As<int?>() == null)
                return null;
            return (a.As<int>() - b.As<int>()).As<Int32_Partial>();
        }

        public static Int32_Partial? operator -(Int32_Partial? a, Int32_Partial? b)
        {
            if (a.As<int?>() == null || b.As<int?>() == null)
                return null;
            return (a.As<int>() - b.As<int>()).As<Int32_Partial>();
        }

        public static Int32_Partial? operator *(Int32_Partial a, Int32_Partial? b)
        {
            if (b.As<int?>() == null)
                return null;
            return (a.As<int>() * b.As<int>()).As<Int32_Partial>();
        }

        public static Int32_Partial? operator *(Int32_Partial? a, Int32_Partial? b)
        {
            if (a.As<int?>() == null || b.As<int?>() == null)
                return null;
            return (a.As<int>() * b.As<int>()).As<Int32_Partial>();
        }

        public static Int32_Partial? operator /(Int32_Partial a, Int32_Partial? b)
        {
            if (b.As<int?>() == null)
                return null;
            return (a.As<int>() / b.As<int>()).As<Int32_Partial>();
        }

        public static Int32_Partial? operator /(Int32_Partial? a, Int32_Partial? b)
        {
            if (a.As<int?>() == null || b.As<int?>() == null)
                return null;
            return (a.As<int>() / b.As<int>()).As<Int32_Partial>();
        }

        public static bool operator >(Int32_Partial a, Int32_Partial? b)
        {
            if (b.As<int?>() == null)
                return false;
            return a.As<int>() > b.As<int>();
        }

        public static bool operator >(Int32_Partial? a, Int32_Partial? b)
        {
            if (a.As<int?>() == null || b.As<int?>() == null)
                return false;
            return a.As<int>() > b.As<int>();
        }

        public static bool operator <(Int32_Partial a, Int32_Partial? b)
        {
            if (b.As<int?>() == null)
                return false;
            return a.As<int>() < b.As<int>();
        }

        public static bool operator <(Int32_Partial? a, Int32_Partial? b)
        {
            if (a.As<int?>() == null || b.As<int?>() == null)
                return false;
            return a.As<int>() < b.As<int>();
        }

        public static bool operator >=(Int32_Partial a, Int32_Partial? b)
        {
            if (b.As<int?>() == null)
                return false;
            return a.As<int>() >= b.As<int>();
        }

        public static bool operator >=(Int32_Partial? a, Int32_Partial? b)
        {
            if (a.As<int?>() == null || b.As<int?>() == null)
                return false;
            return a.As<int>() >= b.As<int>();
        }

        public static bool operator <=(Int32_Partial a, Int32_Partial? b)
        {
            if (b.As<int?>() == null)
                return false;
            return a.As<int>() <= b.As<int>();
        }

        public static bool operator <=(Int32_Partial? a, Int32_Partial? b)
        {
            if (a.As<int?>() == null || b.As<int?>() == null)
                return false;
            return a.As<int>() <= b.As<int>();
        }
    }
}
