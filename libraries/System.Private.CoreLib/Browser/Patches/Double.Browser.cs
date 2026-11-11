namespace System
{
    [NetJs.ForcePartial(typeof(Double))]
    [NetJs.StaticCallConvention]
    public readonly struct Double_Partial
    {
        readonly double _m_value;
        [NetJs.MemberReplace("m_value")]
        internal double MValue
        {
            get
            {
                if (NetJs.Script.TypeOf(this).NativeEquals("number"))
                    return this.As<double>();
                return _m_value;
            }
            set
            {
                NetJs.Script.Write("this._m_value = value");
            }
        }

        public static Double_Partial? operator +(Double_Partial a, Double_Partial? b)
        {
            if (b.As<double?>() == null)
                return null;
            return (a.As<double>() + b.As<double>()).As<Double_Partial>();
        }

        public static Double_Partial? operator +(Double_Partial? a, Double_Partial? b)
        {
            if (a.As<double?>() == null || b.As<double?>() == null)
                return null;
            return (a.As<double>() + b.As<double>()).As<Double_Partial>();
        }

        public static Double_Partial? operator -(Double_Partial a, Double_Partial? b)
        {
            if (b.As<double?>() == null)
                return null;
            return (a.As<double>() - b.As<double>()).As<Double_Partial>();
        }

        public static Double_Partial? operator -(Double_Partial? a, Double_Partial? b)
        {
            if (a.As<double?>() == null || b.As<double?>() == null)
                return null;
            return (a.As<double>() - b.As<double>()).As<Double_Partial>();
        }

        public static Double_Partial? operator *(Double_Partial a, Double_Partial? b)
        {
            if (b.As<double?>() == null)
                return null;
            return (a.As<double>() * b.As<double>()).As<Double_Partial>();
        }

        public static Double_Partial? operator *(Double_Partial? a, Double_Partial? b)
        {
            if (a.As<double?>() == null || b.As<double?>() == null)
                return null;
            return (a.As<double>() * b.As<double>()).As<Double_Partial>();
        }

        public static Double_Partial? operator /(Double_Partial a, Double_Partial? b)
        {
            if (b.As<double?>() == null)
                return null;
            return (a.As<double>() / b.As<double>()).As<Double_Partial>();
        }

        public static Double_Partial? operator /(Double_Partial? a, Double_Partial? b)
        {
            if (a.As<double?>() == null || b.As<double?>() == null)
                return null;
            return (a.As<double>() / b.As<double>()).As<Double_Partial>();
        }

        public static bool operator >(Double_Partial a, Double_Partial? b)
        {
            if (b.As<double?>() == null)
                return false;
            return a.As<double>() > b.As<double>();
        }

        public static bool operator >(Double_Partial? a, Double_Partial? b)
        {
            if (a.As<double?>() == null || b.As<double?>() == null)
                return false;
            return a.As<double>() > b.As<double>();
        }

        public static bool operator <(Double_Partial a, Double_Partial? b)
        {
            if (b.As<double?>() == null)
                return false;
            return a.As<double>() < b.As<double>();
        }

        public static bool operator <(Double_Partial? a, Double_Partial? b)
        {
            if (a.As<double?>() == null || b.As<double?>() == null)
                return false;
            return a.As<double>() < b.As<double>();
        }

        public static bool operator >=(Double_Partial a, Double_Partial? b)
        {
            if (b.As<double?>() == null)
                return false;
            return a.As<double>() >= b.As<double>();
        }

        public static bool operator >=(Double_Partial? a, Double_Partial? b)
        {
            if (a.As<double?>() == null || b.As<double?>() == null)
                return false;
            return a.As<double>() >= b.As<double>();
        }

        public static bool operator <=(Double_Partial a, Double_Partial? b)
        {
            if (b.As<double?>() == null)
                return false;
            return a.As<double>() <= b.As<double>();
        }

        public static bool operator <=(Double_Partial? a, Double_Partial? b)
        {
            if (a.As<double?>() == null || b.As<double?>() == null)
                return false;
            return a.As<double>() <= b.As<double>();
        }
    }
}
