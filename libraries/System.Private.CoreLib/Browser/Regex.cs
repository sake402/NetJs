
namespace NetJs
{
    [NetJs.External]
    [NetJs.Name("RegExp")]
    [NetJs.Convention(Member = NetJs.ConventionMember.Field | NetJs.ConventionMember.Method | NetJs.ConventionMember.Property, Notation = NetJs.Notation.CamelCase)]
    public class RegExp
    {
        [Template("new RegExp({pattern})")]
        public extern RegExp(string pattern);

        [Template("new RegExp({pattern}, {flags})")]
        public extern RegExp(string pattern, string flags);


        public extern int LastIndex
        {
            get;
            set;
        }

        public extern bool Global
        {
            get;
        }

        public extern bool IgnoreCase
        {
            get;
        }

        public extern bool Multiline
        {
            get;
        }

        public extern string Source
        {
            get;
        }

        public extern RegexMatch Exec(string? s);

        public extern bool Test(string? s);
    }

    [NetJs.External]
    [NetJs.Name("RegexMatch")]
    [NetJs.Convention(Member = NetJs.ConventionMember.Field | NetJs.ConventionMember.Method | NetJs.ConventionMember.Property, Notation = NetJs.Notation.CamelCase)]
    public class RegexMatch
    {
        public int Index { get; set; }

        public int Length { get; set; }

        public string Input { get; set; }

        public string this[int index] { get { return null; } set { } }

        public static extern implicit operator string[] (RegexMatch rm);

        public static extern explicit operator RegexMatch(string[] a);
    }
}