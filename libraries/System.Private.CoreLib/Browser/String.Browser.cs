using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace System
{
    [NetJs.StaticCallConvention]
    public partial class String
    {
        [NetJs.Template("{this}.substr({startIndex})")]
        public extern string NativeSubstring(int startIndex);
        [NetJs.Template("{this}.substr({startIndex}, {length})")]
        public extern string NativeSubstring(int startIndex, int length);
        [NetJs.Template("{this}.indexOf({value})")]
        public extern int NativeIndexOf(string value);
        [NetJs.Template("{this}.indexOf({value}, {start})")]
        public extern int NativeIndexOf(string value, int start);
        [NetJs.Template("{this}.replace({pattern}, {value})")]
        public extern string NativeReplace(NetJs.Union<string, RegExp> pattern, string value);
        [NetJs.Template("{this}.replaceAll({pattern}, {value})")]
        public extern string NativeReplaceAll(NetJs.Union<string, RegExp> pattern, string value);
        [NetJs.Template("{this}.split({separator})")]
        public extern string[] NativeSplit(NetJs.Union<string, RegExp> separator);
        [NetJs.Template("{this}.startsWith({pattern})")]
        public extern bool NativeStartsWith(string pattern);
        [NetJs.Template("{this}.endsWith({pattern})")]
        public extern bool NativeEndsWith(string pattern);

        [NetJs.Template("String.fromCharCode({code})")]
        public static extern string NativeFromCharCode(int code);
        [NetJs.Template("String.fromCharCode({code1}, {code2})")]
        public static extern string NativeFromCharCode(int code1, int code2);
        [NetJs.Template("String.fromCharCode.apply(null, {codes})")]
        public static extern string NativeFromCharCode(int[] codes);
        [NetJs.Template("String.fromCharCode.apply(null, {codes})")]
        public static extern string NativeFromCharCode(char[] codes);
        [NetJs.Template("{this}.charCodeAt({i})")]
        public extern char NativeCharCodeAt(int i);
        [NetJs.Template("{this} == {b}")]
        public extern bool NativeEquals(string b);
        [NetJs.Template("{this}.toLowerCase()")]
        public extern string NativeToLower();
        [NetJs.Template("{this}.toUpperCase()")]
        public extern string NativeToUpper();

        [NetJs.MemberReplace(nameof(Empty))]
        public static readonly string EmptyImpl = "";

        [NetJs.Name(NetJs.Constants.IsTypeName)]
        public static bool Is(object? value, out string? result)
        {
            result = NetJs.Script.Write<string>("undefined");
            if (value == null)
                return false;
            if (NetJs.Script.TypeOf(value).NativeEquals("string"))
                return true;
            if (NetJs.Script.InstanceOf(value, typeof(string))) //boxed string
            {
                result = NetJs.Script.Write<string>("value");
                return true;
            }
            return false;
        }

        [NetJs.MemberReplace(nameof(ToString))]
        public string SToStringImpl()
        {
            var value = NetJs.Script.Write<string>("this");
            if (NetJs.Script.TypeOf(value).NativeEquals("string"))
                return value;
            if (NetJs.Script.InstanceOf(value, typeof(string))) //boxed string
            {
                return NetJs.Script.Write<string>("value.m_value");
            }
            throw null!;
        }

        [NetJs.MemberReplace("_stringLength")]
        [NetJs.Template("{this}.length")]
        private int StringLength;
        //{
        //    [NetJs.Template("{this}.length")]
        //    get;
        //}

        [NetJs.MemberReplace("_firstChar")]
        //[NetJs.Template("{this}.charCodeAt(0)")]
        private char FirstChar;
        //{
        //    get
        //    {
        //        return this.NativeCharCodeAt(0);
        //    }
        //    set
        //    {
        //        //NetJs.Script.Write("this._m_value = value");
        //    }
        //}

        public static string Create(char[]? value)
        {
            return Ctor(value);
        }

        public static string Create(char[] value, int startIndex, int length)
        {
            return Ctor(value, startIndex, length);
        }

        public static unsafe string Create(char* value)
        {
            return Ctor(value);
        }

        public static unsafe string Create(char* value, int startIndex, int length)
        {
            return Ctor(value, startIndex, length);
        }

        public static unsafe string Create(sbyte* value)
        {
            return Ctor(value);
        }

        public static unsafe string Create(sbyte* value, int startIndex, int length)
        {
            return Ctor(value, startIndex, length);
        }

        public static string Create(char c, int count)
        {
            return Ctor(c, count);
        }

        public static string Create(ReadOnlySpan<char> value)
        {
            return Ctor(value);
        }

        [NetJs.MemberReplace(nameof(Length))]
        [NetJs.StaticCallConvention(false)]
        [NetJs.Name("length")]
        public extern int LengthImpl
        {
            [NetJs.Template("{this}.length")]
            get;
        }
        public static bool IsProxy(string s) => s["$isProxy"].As<bool>() == true;
        public static StringProxyHandler EnsureIsProxy(string s)
        {
            if (s["$isProxy"].As<bool>() == true)
            {
                return s.As<StringProxyHandler>();
            }
            return JSProxy.Create<StringProxyHandler>(new StringProxyHandler(s));
        }

        [NetJs.MemberReplace(nameof(FastAllocateString))]
        internal static string FastAllocateStringImpl(int length)
        {
            return JSProxy.Create<string>(new StringProxyHandler(length));
        }

        [NetJs.MemberReplace(nameof(InternalIsInterned))]
        private static string InternalIsInternedImpl(string str)
        {
            return str;
        }

        [NetJs.MemberReplace(nameof(InternalIntern))]
        private static string InternalInternImpl(string str)
        {
            return str;
        }


        // Gets the character at a specified position.
        //
        [NetJs.MemberReplace("this[int].get")]
        public char GetChar(int index)
        {
            if ((uint)index >= (uint)this.Length)
                ThrowHelper.ThrowIndexOutOfRangeException();
            //return Unsafe.Add(ref _firstChar, (nint)(uint)index /* force zero-extension */);
            return this.NativeCharCodeAt(index);
        }
        [NetJs.MemberReplace(nameof(GetRawStringData))]
        internal ref char GetRawStringDataImpl()
        {
            if (IsProxy(this))
            {
                var rref = this.As<StringProxyHandler>().Reference;
                NetJs.Script.Write("return rref");
                throw null!;
            }
            else
            {
                var array = NetJs.Script.Write<char[]>("Array.from(this, char => char.charCodeAt(0))");
                Array.AddMetadata(array, typeof(char));
                var rref = RuntimeHelpers.CreateArrayReference(array);
                rref["$originalString"] = this;
                NetJs.Script.Write("return rref");
                throw null!;
            }
        }

        [NetJs.MemberReplace(nameof(GetRawStringDataAsUInt8))]
        internal ref byte GetRawStringDataAsUInt8Impl()
        {
            if (IsProxy(this))
            {
                var lref = this.As<StringProxyHandler>().Reference;
                return ref Unsafe.As<char, byte>(ref NetJs.Script.Ref<char>(lref));
            }
            else
            {
                var array = NetJs.Script.Write<char[]>("Array.from(this, char => char.charCodeAt(0))");
                Array.AddMetadata(array, typeof(char));
                var bArray = new byte[array.Length * 2];
                unchecked
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        bArray[i * 2] = (array[i] & 0xFF).As<byte>();
                        bArray[i * 2 + 1] = ((array[i] >> 8) & 0xFF).As<byte>();
                    }
                }
                var rref = RuntimeHelpers.CreateArrayReference(bArray);
                NetJs.Script.Write("return rref");
                throw null!;
            }
        }

        [NetJs.MemberReplace(nameof(GetRawStringDataAsUInt16))]
        internal ref ushort GetRawStringDataAsUInt16Impl()
        {
            if (IsProxy(this))
            {
                var lref = this.As<StringProxyHandler>().Reference;
                NetJs.Script.Write("return lref");
                throw null!;
            }
            else
            {
                var array = NetJs.Script.Write<char[]>("Array.from(this, char => char.charCodeAt(0))");
                Array.AddMetadata(array, typeof(char));
                var rref = RuntimeHelpers.CreateArrayReference(array);
                NetJs.Script.Write("return rref");
                throw null!;
            }
        }

        [NetJs.MemberReplace(nameof(ToCharArray) + "()")]
        public char[] ToCharArrayImpl()
        {
            char[] array;
            if (IsProxy(this))
            {
                var lref = this.As<StringProxyHandler>();
                array = lref._chars;
            }
            else
            {
                array = NetJs.Script.Write<char[]>("Array.from(this, char => char.charCodeAt(0))");
            }
            return RuntimeHelpers.CreateArrayT<char>(array);
        }

        [NetJs.MemberReplace(nameof(ToCharArray) + "(int, int)")]
        public char[] ToCharArray2Impl(int startIndex, int length)
        {
            char[] array;
            if (IsProxy(this))
            {
                var lref = this.As<StringProxyHandler>();
                array = lref._chars;
            }
            else
            {
                array = NetJs.Script.Write<char[]>("Array.from(this, char => char.charCodeAt(0))");
            }
            return RuntimeHelpers.CreateArrayT<char>(array.Slice(startIndex, length).As<char[]>());
        }

        [NetJs.Template("window.String.fromCharCode({code})")]
        public static extern string LocalNativeFromCharCode(int code);
        public static string operator +(string a, char b)
        {
            return a + LocalNativeFromCharCode(b);
        }
    }
}
