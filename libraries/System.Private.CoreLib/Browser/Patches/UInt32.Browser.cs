
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

        public static UInt32_Partial? operator +(UInt32_Partial a, UInt32_Partial? b)
        {
            if (b.As<uint?>() == null)
                return null;
            return (a.As<uint>() + b.As<uint>()).As<UInt32_Partial>();
        }

        public static UInt32_Partial? operator +(UInt32_Partial? a, UInt32_Partial? b)
        {
            if (a.As<uint?>() == null || b.As<uint?>() == null)
                return null;
            return (a.As<uint>() + b.As<uint>()).As<UInt32_Partial>();
        }

        public static UInt32_Partial? operator -(UInt32_Partial a, UInt32_Partial? b)
        {
            if (b.As<uint?>() == null)
                return null;
            return (a.As<uint>() - b.As<uint>()).As<UInt32_Partial>();
        }

        public static UInt32_Partial? operator -(UInt32_Partial? a, UInt32_Partial? b)
        {
            if (a.As<uint?>() == null || b.As<uint?>() == null)
                return null;
            return (a.As<uint>() - b.As<uint>()).As<UInt32_Partial>();
        }

        public static UInt32_Partial? operator *(UInt32_Partial a, UInt32_Partial? b)
        {
            if (b.As<uint?>() == null)
                return null;
            return (a.As<uint>() * b.As<uint>()).As<UInt32_Partial>();
        }

        public static UInt32_Partial? operator *(UInt32_Partial? a, UInt32_Partial? b)
        {
            if (a.As<uint?>() == null || b.As<uint?>() == null)
                return null;
            return (a.As<uint>() * b.As<uint>()).As<UInt32_Partial>();
        }

        public static UInt32_Partial? operator /(UInt32_Partial a, UInt32_Partial? b)
        {
            if (b.As<uint?>() == null)
                return null;
            return (a.As<uint>() / b.As<uint>()).As<UInt32_Partial>();
        }

        public static UInt32_Partial? operator /(UInt32_Partial? a, UInt32_Partial? b)
        {
            if (a.As<uint?>() == null || b.As<uint?>() == null)
                return null;
            return (a.As<uint>() / b.As<uint>()).As<UInt32_Partial>();
        }

        public static bool operator >(UInt32_Partial a, UInt32_Partial? b)
        {
            if (b.As<uint?>() == null)
                return false;
            return a.As<uint>() > b.As<uint>();
        }

        public static bool operator >(UInt32_Partial? a, UInt32_Partial? b)
        {
            if (a.As<uint?>() == null || b.As<uint?>() == null)
                return false;
            return a.As<uint>() > b.As<uint>();
        }

        public static bool operator <(UInt32_Partial a, UInt32_Partial? b)
        {
            if (b.As<uint?>() == null)
                return false;
            return a.As<uint>() < b.As<uint>();
        }

        public static bool operator <(UInt32_Partial? a, UInt32_Partial? b)
        {
            if (a.As<uint?>() == null || b.As<uint?>() == null)
                return false;
            return a.As<uint>() < b.As<uint>();
        }

        public static bool operator >=(UInt32_Partial a, UInt32_Partial? b)
        {
            if (b.As<uint?>() == null)
                return false;
            return a.As<uint>() >= b.As<uint>();
        }

        public static bool operator >=(UInt32_Partial? a, UInt32_Partial? b)
        {
            if (a.As<uint?>() == null || b.As<uint?>() == null)
                return false;
            return a.As<uint>() >= b.As<uint>();
        }

        public static bool operator <=(UInt32_Partial a, UInt32_Partial? b)
        {
            if (b.As<uint?>() == null)
                return false;
            return a.As<uint>() <= b.As<uint>();
        }

        public static bool operator <=(UInt32_Partial? a, UInt32_Partial? b)
        {
            if (a.As<uint?>() == null || b.As<uint?>() == null)
                return false;
            return a.As<uint>() <= b.As<uint>();
        }
        public static UInt32_Partial? operator &(UInt32_Partial a, UInt32_Partial? b)
        {
            if (b.As<uint?>() == null)
                return null;
            return (a.As<uint>() & b.As<uint>()).As<UInt32_Partial>();
        }

        public static UInt32_Partial? operator &(UInt32_Partial? a, UInt32_Partial? b)
        {
            if (a.As<uint?>() == null || b.As<uint?>() == null)
                return null;
            return (a.As<uint>() & b.As<uint>()).As<UInt32_Partial>();
        }

        public static UInt32_Partial? operator |(UInt32_Partial a, UInt32_Partial? b)
        {
            if (b.As<uint?>() == null)
                return null;
            return (a.As<uint>() | b.As<uint>()).As<UInt32_Partial>();
        }

        public static UInt32_Partial? operator |(UInt32_Partial? a, UInt32_Partial? b)
        {
            if (a.As<uint?>() == null || b.As<uint?>() == null)
                return null;
            return (a.As<uint>() | b.As<uint>()).As<UInt32_Partial>();
        }
    }
}