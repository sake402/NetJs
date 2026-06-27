namespace System
{
    public partial class Math
    {
        [NetJs.Template("Math.random()")]
        public static extern double Random();

        [NetJs.MemberReplace(nameof(Acos) + "(double)")]
        [NetJs.Template("Math.acos({d})")]
        public static extern double AcosImpl(double d);

        [NetJs.MemberReplace(nameof(Acosh) + "(double)")]
        [NetJs.Template("Math.acosh({d})")]
        public static extern double AcoshImpl(double d);

        [NetJs.MemberReplace(nameof(Asin) + "(double)")]
        [NetJs.Template("Math.asin({d})")]
        public static extern double AsinImpl(double d);

        [NetJs.MemberReplace(nameof(Asinh) + "(double)")]
        [NetJs.Template("Math.asinh({d})")]
        public static extern double AsinhImpl(double d);

        [NetJs.MemberReplace(nameof(Atan) + "(double)")]
        [NetJs.Template("Math.atan({d})")]
        public static extern double AtanImpl(double d);

        [NetJs.MemberReplace(nameof(Atan2) + "(double, double)")]
        [NetJs.Template("Math.atan2({y}, {x})")]
        public static extern double Atan2Impl(double y, double x);

        [NetJs.MemberReplace(nameof(Atanh) + "(double)")]
        [NetJs.Template("Math.atanh({d})")]
        public static extern double AtanhImpl(double d);

        [NetJs.MemberReplace(nameof(Cbrt) + "(double)")]
        [NetJs.Template("Math.cbrt({d})")]
        public static extern double CbrtImpl(double d);

        [NetJs.MemberReplace(nameof(Ceiling) + "(double)")]
        [NetJs.Template("Math.ceil({a})")]
        public static extern double CeilingImpl(double a);

        [NetJs.MemberReplace(nameof(Cos) + "(double)")]
        [NetJs.Template("Math.cos({d})")]
        public static extern double CosImpl(double d);

        [NetJs.MemberReplace(nameof(Cosh) + "(double)")]
        [NetJs.Template("Math.cosh({value})")]
        public static extern double CoshImpl(double value);

        [NetJs.MemberReplace(nameof(Exp) + "(double)")]
        [NetJs.Template("Math.exp({d})")]
        public static extern double ExpImpl(double d);

        [NetJs.MemberReplace(nameof(Floor) + "(double)")]
        [NetJs.Template("Math.floor({d})")]
        public static extern double FloorImpl(double d);

        [NetJs.MemberReplace(nameof(Log) + "(double)")]
        [NetJs.Template("Math.log({d})")]
        public static extern double LogImpl(double d);

        [NetJs.MemberReplace(nameof(Log10) + "(double)")]
        [NetJs.Template("Math.log10({d})")]
        public static extern double Log10Impl(double d);

        [NetJs.MemberReplace(nameof(Pow) + "(double, double)")]
        [NetJs.Template("Math.pow({x}, {y})")]
        public static extern double PowImpl(double x, double y);

        [NetJs.MemberReplace(nameof(Sin) + "(double)")]
        [NetJs.Template("Math.sin({a})")]
        public static extern double SinImpl(double a);

        [NetJs.MemberReplace(nameof(Sinh) + "(double)")]
        [NetJs.Template("Math.sinh({value})")]
        public static extern double SinhImpl(double value);

        [NetJs.MemberReplace(nameof(Sqrt) + "(double)")]
        [NetJs.Template("Math.sqrt({d})")]
        public static extern double SqrtImpl(double d);

        [NetJs.MemberReplace(nameof(Tan) + "(double)")]
        [NetJs.Template("Math.tan({a})")]
        public static extern double TanImpl(double a);

        [NetJs.MemberReplace(nameof(Tanh) + "(double)")]
        [NetJs.Template("Math.tanh({value})")]
        public static extern double TanhImpl(double value);

        [NetJs.MemberReplace(nameof(FusedMultiplyAdd) + "(double, double, double)")]
        [NetJs.Template("({x} * {y}) + {z}")]
        public static extern double FusedMultiplyAddImpl(double x, double y, double z);

        [NetJs.MemberReplace(nameof(Log2) + "(double)")]
        [NetJs.Template("Math.log2")]
        public static extern double Log2Impl(double x);

        [NetJs.MemberReplace(nameof(ModF))]
        //[NetJs.Template("Math.truc(x)")]
        private static unsafe double ModFImpl(double x, double* intptr)
        {
            var dd = NetJs.Script.Write<double>("window.Math.ceil(x)");
            *intptr = dd;
            return dd;
        }
    }
}
