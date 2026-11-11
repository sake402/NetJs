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
        public static UInt16_Partial? operator +(UInt16_Partial a, UInt16_Partial? b)
        {
            if (b.As<short?>() == null)
                return null;
            return (a.As<short>() + b.As<short>()).As<UInt16_Partial>();
        }

        public static UInt16_Partial? operator +(UInt16_Partial? a, UInt16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return null;
            return (a.As<short>() + b.As<short>()).As<UInt16_Partial>();
        }

        public static UInt16_Partial? operator -(UInt16_Partial a, UInt16_Partial? b)
        {
            if (b.As<short?>() == null)
                return null;
            return (a.As<short>() - b.As<short>()).As<UInt16_Partial>();
        }

        public static UInt16_Partial? operator -(UInt16_Partial? a, UInt16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return null;
            return (a.As<short>() - b.As<short>()).As<UInt16_Partial>();
        }

        public static UInt16_Partial? operator *(UInt16_Partial a, UInt16_Partial? b)
        {
            if (b.As<short?>() == null)
                return null;
            return (a.As<short>() * b.As<short>()).As<UInt16_Partial>();
        }

        public static UInt16_Partial? operator *(UInt16_Partial? a, UInt16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return null;
            return (a.As<short>() * b.As<short>()).As<UInt16_Partial>();
        }

        public static UInt16_Partial? operator /(UInt16_Partial a, UInt16_Partial? b)
        {
            if (b.As<short?>() == null)
                return null;
            return (a.As<short>() / b.As<short>()).As<UInt16_Partial>();
        }

        public static UInt16_Partial? operator /(UInt16_Partial? a, UInt16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return null;
            return (a.As<short>() / b.As<short>()).As<UInt16_Partial>();
        }

        public static bool operator >(UInt16_Partial a, UInt16_Partial? b)
        {
            if (b.As<short?>() == null)
                return false;
            return a.As<short>() > b.As<short>();
        }

        public static bool operator >(UInt16_Partial? a, UInt16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return false;
            return a.As<short>() > b.As<short>();
        }

        public static bool operator <(UInt16_Partial a, UInt16_Partial? b)
        {
            if (b.As<short?>() == null)
                return false;
            return a.As<short>() < b.As<short>();
        }

        public static bool operator <(UInt16_Partial? a, UInt16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return false;
            return a.As<short>() < b.As<short>();
        }

        public static bool operator >=(UInt16_Partial a, UInt16_Partial? b)
        {
            if (b.As<short?>() == null)
                return false;
            return a.As<short>() >= b.As<short>();
        }

        public static bool operator >=(UInt16_Partial? a, UInt16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return false;
            return a.As<short>() >= b.As<short>();
        }

        public static bool operator <=(UInt16_Partial a, UInt16_Partial? b)
        {
            if (b.As<short?>() == null)
                return false;
            return a.As<short>() <= b.As<short>();
        }

        public static bool operator <=(UInt16_Partial? a, UInt16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return false;
            return a.As<short>() <= b.As<short>();
        }

        public static UInt16_Partial? operator &(UInt16_Partial a, UInt16_Partial? b)
        {
            if (b.As<short?>() == null)
                return null;
            return (a.As<short>() & b.As<short>()).As<UInt16_Partial>();
        }

        public static UInt16_Partial? operator &(UInt16_Partial? a, UInt16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return null;
            return (a.As<short>() & b.As<short>()).As<UInt16_Partial>();
        }

        public static UInt16_Partial? operator |(UInt16_Partial a, UInt16_Partial? b)
        {
            if (b.As<short?>() == null)
                return null;
            return (a.As<short>() | b.As<short>()).As<UInt16_Partial>();
        }

        public static UInt16_Partial? operator |(UInt16_Partial? a, UInt16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return null;
            return (a.As<short>() | b.As<short>()).As<UInt16_Partial>();
        }
    }
}
