namespace System.Runtime.CompilerServices
{
    [NetJs.ForcePartial(typeof(JitHelpers))]
    internal static partial class JitHelpers_Partial
    {
        [NetJs.MemberReplace(nameof(JitHelpers.EnumEquals) + "<>")]
        [NetJs.Template("{x} === {y}")]
        public static extern bool EnumEquals<T>(T x, T y) where T : struct, Enum;// => x.As<int>() == y.As<int>();

        [NetJs.MemberReplace(nameof(JitHelpers.EnumCompareTo) + "<>")]
        [NetJs.Template("Number({x} - {y})")]
        public static extern int EnumCompareTo<T>(T x, T y) where T : struct, Enum;//=> (int)(x.As<long>() - y.As<long>());
    }
}
