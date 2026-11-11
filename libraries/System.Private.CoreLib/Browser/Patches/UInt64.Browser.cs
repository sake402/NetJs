
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

        //[NetJs.Name(Constants.IsTypeName)]
        //public static bool Is(object value)
        //{
        //    return NetJs.Script.TypeOf(value).NativeEquals("bigint");
        //}
        public static UInt64_Partial? operator +(UInt64_Partial a, UInt64_Partial? b)
        {
            if (b.As<ulong?>() == null)
                return null;
            return (a.As<ulong>() + b.As<ulong>()).As<UInt64_Partial>();
        }

        public static UInt64_Partial? operator +(UInt64_Partial? a, UInt64_Partial? b)
        {
            if (a.As<ulong?>() == null || b.As<ulong?>() == null)
                return null;
            return (a.As<ulong>() + b.As<ulong>()).As<UInt64_Partial>();
        }

        public static UInt64_Partial? operator -(UInt64_Partial a, UInt64_Partial? b)
        {
            if (b.As<ulong?>() == null)
                return null;
            return (a.As<ulong>() - b.As<ulong>()).As<UInt64_Partial>();
        }

        public static UInt64_Partial? operator -(UInt64_Partial? a, UInt64_Partial? b)
        {
            if (a.As<ulong?>() == null || b.As<ulong?>() == null)
                return null;
            return (a.As<ulong>() - b.As<ulong>()).As<UInt64_Partial>();
        }

        public static UInt64_Partial? operator *(UInt64_Partial a, UInt64_Partial? b)
        {
            if (b.As<ulong?>() == null)
                return null;
            return (a.As<ulong>() * b.As<ulong>()).As<UInt64_Partial>();
        }

        public static UInt64_Partial? operator *(UInt64_Partial? a, UInt64_Partial? b)
        {
            if (a.As<ulong?>() == null || b.As<ulong?>() == null)
                return null;
            return (a.As<ulong>() * b.As<ulong>()).As<UInt64_Partial>();
        }

        public static UInt64_Partial? operator /(UInt64_Partial a, UInt64_Partial? b)
        {
            if (b.As<ulong?>() == null)
                return null;
            return (a.As<ulong>() / b.As<ulong>()).As<UInt64_Partial>();
        }

        public static UInt64_Partial? operator /(UInt64_Partial? a, UInt64_Partial? b)
        {
            if (a.As<ulong?>() == null || b.As<ulong?>() == null)
                return null;
            return (a.As<ulong>() / b.As<ulong>()).As<UInt64_Partial>();
        }

        public static bool operator >(UInt64_Partial a, UInt64_Partial? b)
        {
            if (b.As<ulong?>() == null)
                return false;
            return a.As<ulong>() > b.As<ulong>();
        }

        public static bool operator >(UInt64_Partial? a, UInt64_Partial? b)
        {
            if (a.As<ulong?>() == null || b.As<ulong?>() == null)
                return false;
            return a.As<ulong>() > b.As<ulong>();
        }

        public static bool operator <(UInt64_Partial a, UInt64_Partial? b)
        {
            if (b.As<ulong?>() == null)
                return false;
            return a.As<ulong>() < b.As<ulong>();
        }

        public static bool operator <(UInt64_Partial? a, UInt64_Partial? b)
        {
            if (a.As<ulong?>() == null || b.As<ulong?>() == null)
                return false;
            return a.As<ulong>() < b.As<ulong>();
        }

        public static bool operator >=(UInt64_Partial a, UInt64_Partial? b)
        {
            if (b.As<ulong?>() == null)
                return false;
            return a.As<ulong>() >= b.As<ulong>();
        }

        public static bool operator >=(UInt64_Partial? a, UInt64_Partial? b)
        {
            if (a.As<ulong?>() == null || b.As<ulong?>() == null)
                return false;
            return a.As<ulong>() >= b.As<ulong>();
        }

        public static bool operator <=(UInt64_Partial a, UInt64_Partial? b)
        {
            if (b.As<ulong?>() == null)
                return false;
            return a.As<ulong>() <= b.As<ulong>();
        }

        public static bool operator <=(UInt64_Partial? a, UInt64_Partial? b)
        {
            if (a.As<ulong?>() == null || b.As<ulong?>() == null)
                return false;
            return a.As<ulong>() <= b.As<ulong>();
        }
        public static UInt64_Partial? operator &(UInt64_Partial a, UInt64_Partial? b)
        {
            if (b.As<ulong?>() == null)
                return null;
            return (a.As<ulong>() & b.As<ulong>()).As<UInt64_Partial>();
        }

        public static UInt64_Partial? operator &(UInt64_Partial? a, UInt64_Partial? b)
        {
            if (a.As<ulong?>() == null || b.As<ulong?>() == null)
                return null;
            return (a.As<ulong>() & b.As<ulong>()).As<UInt64_Partial>();
        }

        public static UInt64_Partial? operator |(UInt64_Partial a, UInt64_Partial? b)
        {
            if (b.As<ulong?>() == null)
                return null;
            return (a.As<ulong>() | b.As<ulong>()).As<UInt64_Partial>();
        }

        public static UInt64_Partial? operator |(UInt64_Partial? a, UInt64_Partial? b)
        {
            if (a.As<ulong?>() == null || b.As<ulong?>() == null)
                return null;
            return (a.As<ulong>() | b.As<ulong>()).As<UInt64_Partial>();
        }

    }
}
