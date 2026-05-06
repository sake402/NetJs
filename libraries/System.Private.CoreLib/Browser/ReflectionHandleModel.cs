using NetJs;
using System.Text.Json.Serialization;

namespace System
{
    //[ObjectLiteral]
    //public struct ReflectionHandleModel
    //{
    //    public const int AssemblyShift = 0;
    //    public const ulong AssemblyMask = 0xFFFF;
    //    public const int TypeShift = 16;
    //    public const ulong TypeMask = 0xFFFF0000;
    //    public const int MemberShift = 32;
    //    public const ulong MemberMask = 0xFFFF00000000;
    //    [NetJs.Name("v")]
    //    [JsonPropertyName("v")]
    //    public ulong Value { get; set; }
    //    [JsonIgnore]
    //    public int Assembly
    //    {
    //        [NetJs.Template("{this}.v & 0xFFFF")]
    //        get
    //        {
    //            return (int)(Value & AssemblyMask);
    //        }
    //    }

    //    [JsonIgnore]
    //    public int Type
    //    {
    //        [NetJs.Template("({this}.v & 0xFFFF0000) >> 16")]
    //        get
    //        {
    //            return (int)((Value & TypeMask) >> TypeShift);
    //        }
    //    }

    //    [JsonIgnore]
    //    public int Member
    //    {
    //        [NetJs.Template("Number((BigInt({this}.v) & 0xFFFF00000000n) >> 32n)")]
    //        get
    //        {
    //            return (int)((Value & MemberMask) >> MemberShift);
    //        }
    //    }

    //    [JsonIgnore]
    //    public int AssemblyAndType
    //    {
    //        [NetJs.Template("({this}.v & (0xFFFF | 0xFFFF0000))")]
    //        get
    //        {
    //            return (int)(Value & (AssemblyMask | TypeMask));
    //        }
    //    }
    //}

    public static class ReflectionHandleExtension
    {

        public const int AssemblyShift = 0;
        public const ulong AssemblyMask = 0xFFFF;
        public const int TypeShift = 16;
        public const ulong TypeMask = 0xFFFF0000;
        public const int MemberShift = 32;
        public const ulong MemberMask = 0xFFFF00000000;
        [NetJs.Template("{value} & 0xFFFF")]
        public static extern int GetAssemblyHandle(this uint value);// => (int)(value & AssemblyMask);

        [NetJs.Template("({value} & 0xFFFF0000) >> 16")]
        public static extern int GetTypeHandle(this uint value);// => (int)((value & TypeMask) >> TypeShift);

        [NetJs.Template("Number((BigInt({value}) & 0xFFFF00000000n) >> 32n)")]
        public static extern int GetMemberHandle(this uint value);// => (int)((value & MemberMask) >> MemberShift);

        [NetJs.Template("({value} & (0xFFFF | 0xFFFF0000))")]
        public static extern int GetAssemblyAndTypeHandle(this uint value);// => (int)(value & (AssemblyMask | TypeMask));
    }
}