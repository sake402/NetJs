namespace System
{
    [NetJs.ForcePartial(typeof(Single))]
    [NetJs.StaticCallConvention]
    public readonly struct Single_Partial
    {
        readonly float _m_value;
        [NetJs.MemberReplace("m_value")]
        internal float MValue
        {
            get
            {
                if (NetJs.Script.TypeOf(this).NativeEquals("number"))
                    return this.As<float>();
                return _m_value;
            }
            set
            {
                NetJs.Script.Write("this._m_value = value");
            }
        }
        public static Single_Partial? operator +(Single_Partial a, Single_Partial? b)
        {
            if (b.As<float?>() == null)
                return null;
            return (a.As<float>() + b.As<float>()).As<Single_Partial>();
        }

        public static Single_Partial? operator +(Single_Partial? a, Single_Partial? b)
        {
            if (a.As<float?>() == null || b.As<float?>() == null)
                return null;
            return (a.As<float>() + b.As<float>()).As<Single_Partial>();
        }

        public static Single_Partial? operator -(Single_Partial a, Single_Partial? b)
        {
            if (b.As<float?>() == null)
                return null;
            return (a.As<float>() - b.As<float>()).As<Single_Partial>();
        }

        public static Single_Partial? operator -(Single_Partial? a, Single_Partial? b)
        {
            if (a.As<float?>() == null || b.As<float?>() == null)
                return null;
            return (a.As<float>() - b.As<float>()).As<Single_Partial>();
        }

        public static Single_Partial? operator *(Single_Partial a, Single_Partial? b)
        {
            if (b.As<float?>() == null)
                return null;
            return (a.As<float>() * b.As<float>()).As<Single_Partial>();
        }

        public static Single_Partial? operator *(Single_Partial? a, Single_Partial? b)
        {
            if (a.As<float?>() == null || b.As<float?>() == null)
                return null;
            return (a.As<float>() * b.As<float>()).As<Single_Partial>();
        }

        public static Single_Partial? operator /(Single_Partial a, Single_Partial? b)
        {
            if (b.As<float?>() == null)
                return null;
            return (a.As<float>() / b.As<float>()).As<Single_Partial>();
        }

        public static Single_Partial? operator /(Single_Partial? a, Single_Partial? b)
        {
            if (a.As<float?>() == null || b.As<float?>() == null)
                return null;
            return (a.As<float>() / b.As<float>()).As<Single_Partial>();
        }

        public static bool operator >(Single_Partial a, Single_Partial? b)
        {
            if (b.As<float?>() == null)
                return false;
            return a.As<float>() > b.As<float>();
        }

        public static bool operator >(Single_Partial? a, Single_Partial? b)
        {
            if (a.As<float?>() == null || b.As<float?>() == null)
                return false;
            return a.As<float>() > b.As<float>();
        }

        public static bool operator <(Single_Partial a, Single_Partial? b)
        {
            if (b.As<float?>() == null)
                return false;
            return a.As<float>() < b.As<float>();
        }

        public static bool operator <(Single_Partial? a, Single_Partial? b)
        {
            if (a.As<float?>() == null || b.As<float?>() == null)
                return false;
            return a.As<float>() < b.As<float>();
        }

        public static bool operator >=(Single_Partial a, Single_Partial? b)
        {
            if (b.As<float?>() == null)
                return false;
            return a.As<float>() >= b.As<float>();
        }

        public static bool operator >=(Single_Partial? a, Single_Partial? b)
        {
            if (a.As<float?>() == null || b.As<float?>() == null)
                return false;
            return a.As<float>() >= b.As<float>();
        }

        public static bool operator <=(Single_Partial a, Single_Partial? b)
        {
            if (b.As<float?>() == null)
                return false;
            return a.As<float>() <= b.As<float>();
        }

        public static bool operator <=(Single_Partial? a, Single_Partial? b)
        {
            if (a.As<float?>() == null || b.As<float?>() == null)
                return false;
            return a.As<float>() <= b.As<float>();
        }
    }
}
