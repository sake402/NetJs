namespace System
{
    [NetJs.ForcePartial(typeof(Int16))]
    [NetJs.StaticCallConvention]
    public readonly struct Int16_Partial 
    {
        //[NetJs.MemberReplace(nameof(GetHashCode))]
        //public int GetHashCodeImplChar()
        //{
        //    return NetJs.Script.Write<short>("m_value");
        //}

        readonly short _m_value;
        [NetJs.MemberReplace("m_value")]
        internal short MValue
        {
            get
            {
                if (NetJs.Script.TypeOf(this).NativeEquals("number"))
                    return this.As<short>();
                return _m_value;
            }
            set
            {
                NetJs.Script.Write("this._m_value = value");
            }
        }
        public static Int16_Partial? operator +(Int16_Partial a, Int16_Partial? b)
        {
            if (b.As<short?>() == null)
                return null;
            return (a.As<short>() + b.As<short>()).As<Int16_Partial>();
        }

        public static Int16_Partial? operator +(Int16_Partial? a, Int16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return null;
            return (a.As<short>() + b.As<short>()).As<Int16_Partial>();
        }

        public static Int16_Partial? operator -(Int16_Partial a, Int16_Partial? b)
        {
            if (b.As<short?>() == null)
                return null;
            return (a.As<short>() - b.As<short>()).As<Int16_Partial>();
        }

        public static Int16_Partial? operator -(Int16_Partial? a, Int16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return null;
            return (a.As<short>() - b.As<short>()).As<Int16_Partial>();
        }

        public static Int16_Partial? operator *(Int16_Partial a, Int16_Partial? b)
        {
            if (b.As<short?>() == null)
                return null;
            return (a.As<short>() * b.As<short>()).As<Int16_Partial>();
        }

        public static Int16_Partial? operator *(Int16_Partial? a, Int16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return null;
            return (a.As<short>() * b.As<short>()).As<Int16_Partial>();
        }

        public static Int16_Partial? operator /(Int16_Partial a, Int16_Partial? b)
        {
            if (b.As<short?>() == null)
                return null;
            return (a.As<short>() / b.As<short>()).As<Int16_Partial>();
        }

        public static Int16_Partial? operator /(Int16_Partial? a, Int16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return null;
            return (a.As<short>() / b.As<short>()).As<Int16_Partial>();
        }

        public static bool operator >(Int16_Partial a, Int16_Partial? b)
        {
            if (b.As<short?>() == null)
                return false;
            return a.As<short>() > b.As<short>();
        }

        public static bool operator >(Int16_Partial? a, Int16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return false;
            return a.As<short>() > b.As<short>();
        }

        public static bool operator <(Int16_Partial a, Int16_Partial? b)
        {
            if (b.As<short?>() == null)
                return false;
            return a.As<short>() < b.As<short>();
        }

        public static bool operator <(Int16_Partial? a, Int16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return false;
            return a.As<short>() < b.As<short>();
        }

        public static bool operator >=(Int16_Partial a, Int16_Partial? b)
        {
            if (b.As<short?>() == null)
                return false;
            return a.As<short>() >= b.As<short>();
        }

        public static bool operator >=(Int16_Partial? a, Int16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return false;
            return a.As<short>() >= b.As<short>();
        }

        public static bool operator <=(Int16_Partial a, Int16_Partial? b)
        {
            if (b.As<short?>() == null)
                return false;
            return a.As<short>() <= b.As<short>();
        }

        public static bool operator <=(Int16_Partial? a, Int16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return false;
            return a.As<short>() <= b.As<short>();
        }

        public static Int16_Partial? operator &(Int16_Partial a, Int16_Partial? b)
        {
            if (b.As<short?>() == null)
                return null;
            return (a.As<short>() & b.As<short>()).As<Int16_Partial>();
        }

        public static Int16_Partial? operator &(Int16_Partial? a, Int16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return null;
            return (a.As<short>() & b.As<short>()).As<Int16_Partial>();
        }

        public static Int16_Partial? operator |(Int16_Partial a, Int16_Partial? b)
        {
            if (b.As<short?>() == null)
                return null;
            return (a.As<short>() | b.As<short>()).As<Int16_Partial>();
        }

        public static Int16_Partial? operator |(Int16_Partial? a, Int16_Partial? b)
        {
            if (a.As<short?>() == null || b.As<short?>() == null)
                return null;
            return (a.As<short>() | b.As<short>()).As<Int16_Partial>();
        }
    }
}
