using YamlDotNet.Serialization;

namespace NetJs.Translator.CSharpToJavascript
{
    public class ILLinkerAssembly
    {
        public string FullName { get; set; } = default!;
        public string? Feature { get; set; }
        public string? FeatureValue { get; set; }
        public string? FeatureDefault { get; set; }
        public IEnumerable<Type>? Types { get; set; }

        public override string ToString()
        {
            return FullName;
        }

        public class Type
        {
            public string FullName { get; set; } = default!;
            public IEnumerable<Member>? Methods { get; set; }
            public IEnumerable<Member>? Fields { get; set; }
            public IEnumerable<Member>? Properties { get; set; }
            public string? Preserve { get; set; }
            [YamlIgnore]
            public string NormalizedFullName => FullName.Replace("/", ".");
            [YamlIgnore]
            public IEnumerable<Member> Members => (Methods ?? Enumerable.Empty<Member>()).Concat(Fields ?? Enumerable.Empty<Member>()).Concat(Properties ?? Enumerable.Empty<Member>());
            public override string ToString()
            {
                return FullName;
            }

        }

        public enum MemberType
        {
            Method,
            Field,
            Property
        }
        public class Member
        {
            public MemberType MemberType { get; set; }
            public string? Name { get; set; }
            public string? Signature { get; set; }
            public string? Body { get; set; }
            public string? Value { get; set; }

            [YamlIgnore]
            public string? NormalizedSignature
            {
                get
                {
                    if (Signature == null)
                        return null;
                    if (MemberType == MemberType.Method && Signature.EndsWith("()"))
                    {
                        var getIndex = Signature.IndexOf(" get_");
                        if (getIndex > 0)
                        {
                            return Signature.Substring(getIndex + 5).TrimEnd(['(', ')']);
                        }
                    }
                    return Signature;
                }
            }
            public override string ToString()
            {
                return (Name ?? Signature) + " => " + Value;
            }
        }

        //public class Field
        //{
        //    public string? Name { get; set; } = default!;
        //    public string? Signature { get; set; }
        //    public string? Body { get; set; }
        //    public string? Value { get; set; }
        //    public override string ToString()
        //    {
        //        return (Name ?? Signature) + " => " + Value;
        //    }
        //}

        //public class Property
        //{
        //    public string? Name { get; set; } = default!;
        //    public string? Signature { get; set; }
        //    public string? Body { get; set; }
        //    public string? Value { get; set; }
        //    public override string ToString()
        //    {
        //        return (Name ?? Signature) + " => " + Value;
        //    }
        //}
    }
}